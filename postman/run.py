"""Executa a coleção Postman com um ambiente temporário de loopback."""

import argparse
import json
import os
from pathlib import Path
import subprocess
import tempfile


POSTMAN = Path(__file__).resolve().parent
REPOSITORIO = POSTMAN.parent


def ler_codigo_local():
    codigo = os.environ.get("ESCOLA_JWT_CODIGO")
    if codigo:
        return codigo

    for linha in (REPOSITORIO / ".env").read_text(encoding="utf-8").splitlines():
        chave, separador, valor = linha.strip().removeprefix("export ").partition("=")
        if separador and chave.strip() == "ESCOLA_JWT_CODIGO":
            codigo = valor.strip()
            if len(codigo) >= 2 and codigo[0] in {"'", '"'} and codigo[-1] == codigo[0]:
                codigo = codigo[1:-1]
            if codigo:
                return codigo

    raise ValueError("Configure ESCOLA_JWT_CODIGO no .env local.")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--porta", type=int, default=os.environ.get("ESCOLA_PORTA_LOCAL", "5080"))
    args = parser.parse_args()
    if not 1 <= args.porta <= 65535:
        parser.error("--porta deve estar entre 1 e 65535.")

    try:
        codigo = ler_codigo_local()
    except FileNotFoundError:
        parser.error("Crie o .env local conforme o README.")
    except ValueError as erro:
        parser.error(str(erro))

    ambiente = json.loads((POSTMAN / "fiap-5nett-local.postman_environment.json").read_text(encoding="utf-8"))
    valores = {item["key"]: item for item in ambiente["values"]}
    valores["baseUrl"]["value"] = f"http://127.0.0.1:{args.porta}"
    valores["codigoAcesso"]["value"] = codigo

    cli = os.environ.get("POSTMAN_CLI", "postman")
    ambiente_cli = os.environ.copy()
    for chave in ("POSTMAN_API_KEY", "ESCOLA_JWT_CODIGO", "ESCOLA_JWT_CHAVE"):
        ambiente_cli.pop(chave, None)
    with tempfile.TemporaryDirectory(prefix="fiap-postman-") as diretorio:
        arquivo = Path(diretorio) / "fiap-local.postman_environment.json"
        arquivo.touch(mode=0o600)
        arquivo.write_text(json.dumps(ambiente), encoding="utf-8")
        try:
            resultado = subprocess.run(
                [
                    cli, "collection", "run", str(POSTMAN / "fiap-5nett-fase-3.postman_collection.json"),
                    "--environment", str(arquivo), "--no-report-events",
                ],
                env=ambiente_cli,
                check=False,
            )
        except FileNotFoundError:
            parser.error("Abra o terminal do Postman Desktop ou instale o Postman CLI independente.")
    return resultado.returncode


if __name__ == "__main__":
    raise SystemExit(main())
