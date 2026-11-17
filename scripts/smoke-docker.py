import http.client
import json
import os
import secrets
import subprocess
import sys
import time


def request(endpoint, method, path, expected_status, payload=None, token=None):
    headers = {}
    if payload is not None:
        headers["Content-Type"] = "application/json"
    if token is not None:
        headers["Authorization"] = f"Bearer {token}"

    connection = http.client.HTTPConnection(endpoint, timeout=2)
    try:
        body = json.dumps(payload) if payload is not None else None
        connection.request(method, path, body=body, headers=headers)
        response = connection.getresponse()
        if response.status != expected_status:
            raise RuntimeError(
                f"{method} {path}: esperado HTTP {expected_status}, recebido HTTP {response.status}"
            )
        return response.read()
    finally:
        connection.close()


def wait_for_api(endpoint):
    deadline = time.monotonic() + 30
    while True:
        try:
            request(endpoint, "GET", "/openapi/v1.json", 200)
            return
        except OSError as error:
            if time.monotonic() >= deadline:
                raise RuntimeError("A API não iniciou dentro de 30 segundos") from error
            time.sleep(0.5)


def smoke(image):
    access_code = secrets.token_hex(32)
    environment = dict(
        os.environ,
        ASPNETCORE_ENVIRONMENT="Development",
        Jwt__ChaveAssinatura=secrets.token_hex(32),
        Jwt__CodigoAcesso=access_code,
    )
    container = subprocess.check_output(
        [
            "docker", "run", "--detach", "--rm", "--publish", "127.0.0.1::8080",
            "--env", "ASPNETCORE_ENVIRONMENT",
            "--env", "Jwt__ChaveAssinatura",
            "--env", "Jwt__CodigoAcesso",
            image,
        ],
        env=environment,
        text=True,
    ).strip()

    try:
        endpoint = subprocess.check_output(
            ["docker", "port", container, "8080/tcp"], text=True
        ).strip()
        wait_for_api(endpoint)

        path = "/resultados-academicos/calcular"
        grades = {"nota1": 6, "nota2": 8}
        request(endpoint, "POST", path, 401, grades)
        print("Cálculo sem token: HTTP 401", flush=True)

        credentials = json.loads(
            request(endpoint, "POST", "/auth/token", 200, {"codigo": access_code})
        )
        token = credentials.get("accessToken")
        if not isinstance(token, str) or not token:
            raise RuntimeError("A emissão de token retornou um accessToken vazio ou inválido")

        result = json.loads(request(endpoint, "POST", path, 200, grades, token=token))
        if result != {"media": 7, "situacao": "Aprovado"}:
            raise RuntimeError("O cálculo retornou um resultado diferente do contrato esperado")
        print("Cálculo com JWT: HTTP 200, média 7 e situação Aprovado", flush=True)
    finally:
        subprocess.run(["docker", "stop", container], check=True, stdout=subprocess.DEVNULL)


if __name__ == "__main__":
    smoke(sys.argv[1])
