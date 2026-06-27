# Secret Manager

Gerenciador de senhas leve, nativo do Windows, que roda no **system tray** (ao lado do relógio). Tudo é criptografado em repouso, inclusive os backups em pendrive.

## Por que é seguro

| Camada | Escolha |
|--------|---------|
| Derivação de chave | **Argon2id** (64 MiB, 4 passes, paralelismo 2) — resistente a ataque por GPU/ASIC |
| Cifra | **AES-256-GCM** — confidencialidade + integridade autenticada (detecta adulteração) |
| Nonce | 96 bits aleatório por gravação; tag de 128 bits |
| Senha mestra | Nunca gravada. A chave derivada vive só em memória e é **zerada** (`CryptographicOperations.ZeroMemory`) ao travar |
| Gravação | Atômica (temp + replace) — não corrompe em queda de energia |
| Backup | Mesmo formato cifrado do cofre principal — nada sai em texto claro |

> A senha mestra **não pode ser recuperada**. Se esquecê-la, o cofre fica inacessível — esse é o ponto.

## Recursos

- 🔒 Roda no tray; duplo-clique no ícone abre o cofre
- ⏱️ **Auto-lock** por inatividade (configurável; detecta ociosidade via `GetLastInputInfo`)
- 📋 Cópia de senha para clipboard com **limpeza automática** após N segundos
- 🎲 Gerador de senhas fortes (RNG criptográfico, sem caracteres ambíguos)
- 🗂️ **Grupos/categorias** opcionais — a lista agrupa por grupo (credenciais sem grupo vão para "Sem grupo"), com filtro por grupo no topo
- 💾 **Backup criptografado para pendrive** (manual ou automático a cada alteração)
- 🔑 Troca de senha mestra (re-criptografa todo o cofre)
- 🪶 Leve: WinForms nativo, sem Electron; publicável como **único .exe**
- 🚀 Iniciar com o Windows (opcional, via HKCU Run)

## Onde ficam os dados

```
%APPDATA%\SecretManager\
  vault.smgr       cofre criptografado (AES-256-GCM)
  settings.json    preferências não sensíveis (sem segredos)
```

Backups: `vault-backup-AAAAMMDD-HHMMSS.smgr` no destino escolhido.

## Build & execução

Requer .NET SDK 10.

```bash
# rodar em dev
dotnet run --project src/SecretManager

# publicar um único .exe self-contained (não precisa de .NET na máquina alvo)
dotnet publish src/SecretManager -c Release -p:PublishProfile=win-x64
```

O executável final fica em
`src/SecretManager/bin/Release/net10.0-windows/win-x64/publish/SecretManager.exe`.

## Uso

1. No primeiro start, defina a senha mestra (mín. 8 caracteres).
2. O ícone de cadeado aparece no tray. Duplo-clique → janela principal.
3. **Nova** para adicionar credenciais; **Gerar** cria uma senha forte.
4. Duplo-clique numa linha (ou Enter) copia a senha; o clipboard é limpo sozinho.
5. **Backup pendrive** detecta drives removíveis e grava uma cópia cifrada.
6. **Config** ajusta auto-lock, tempo de limpeza do clipboard e início com Windows.

## Estrutura

```
src/SecretManager/
  Program.cs                 entry point + instância única
  Crypto/
    VaultCrypto.cs           Argon2id + AES-256-GCM (formato de arquivo)
    PasswordGenerator.cs     geração de senhas com CSPRNG
  Models/                    Vault, VaultEntry
  Storage/
    VaultStore.cs            load/save/backup, chave em memória
    RemovableDrives.cs       enumera pendrives
    AppSettings.cs           preferências
    StartupRegistration.cs   início com Windows (HKCU Run)
  UI/                        TrayApplicationContext + Forms
```
