# Política de segurança

Este repositório contém um projeto educacional da FIAP 5NETT — Fase 3.
A manutenção ocorre conforme a disponibilidade do mantenedor, sem prazo
garantido de resposta ou correção.

## Versões com manutenção

| Referência | Manutenção de segurança |
| --- | --- |
| Código atual da branch `main` | Relatos avaliados e correções incorporadas nesta branch |
| Commits anteriores, tags e forks | Sem manutenção separada ou backports |

As releases `evidencias-fase3-v1` e `evidencias-fase3-v2` arquivam relatórios
de execuções identificadas. Sua numeração não representa versões da aplicação
com suporte de segurança próprio.

## Como reportar uma vulnerabilidade

Use o [reporte privado de vulnerabilidade do GitHub](https://github.com/robertishimurasousa/Fiap-Pos-Tech-5NETT-Fase3/security/advisories/new).
Esse canal está habilitado no repositório e requer uma conta GitHub.
Também é possível acessá-lo pela aba **Security**, em **Report a vulnerability**.

Inclua no relato:

- Commit ou referência afetada e versões relevantes do ambiente (.NET, Docker ou sistema operacional).
- Componente afetado, passos para reproduzir e uma prova de conceito mínima.
- Comportamento observado, impacto esperado e condições necessárias para explorar a falha.
- Sugestão de correção, se houver.

Use um ambiente local sob seu controle para reproduzir o problema. Remova
tokens, chaves, códigos de acesso, dados pessoais e outros valores sensíveis
dos exemplos. Mantenha detalhes de exploração no canal privado durante a análise.
Issues públicas podem ser usadas para dúvidas gerais, sem expor esses detalhes.

## Acompanhamento e tratamento

O acompanhamento ocorre no próprio relato privado. As atualizações são feitas
quando houver resultado da análise, necessidade de informações adicionais ou
mudança na situação da correção; não há periodicidade fixa de atendimento.

- **Vulnerabilidade confirmada:** o mantenedor avalia o impacto, prepara a correção
  na `main` e combina pelo canal privado a divulgação dos detalhes, quando aplicável.
- **Relato não reproduzido, duplicado ou fora do escopo:** o mantenedor solicita
  informações adicionais ou explica o motivo do encerramento no próprio relato.

A confirmação de recebimento ou a abertura de um relato não implica confirmação
da vulnerabilidade ou prazo de correção.
