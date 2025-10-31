# BankMore

Sistema de fintech desenvolvido em .NET 8 seguindo arquitetura de microsserviços com Domain-Driven Design e padrão CQRS.

## Estrutura do Projeto

O sistema é composto por três microsserviços de API e uma aplicação web Blazor Server:

- Contas API: Gerencia cadastro, autenticação, movimentações e consulta de saldo
- Transferencias API: Realiza transferências entre contas com compensação automática
- Tarifas API: Aplica tarifas automaticamente via mensageria Kafka
- BankMore Web: Interface web desenvolvida em Blazor Server

Cada microsserviço segue a arquitetura em camadas com Domain, Application, Infrastructure e API. O Domain contém as entidades e regras de negócio, Application implementa os handlers de comandos e queries usando MediatR, e Infrastructure cuida dos repositórios, serviços externos e mensageria.

## Tecnologias

- .NET 8
- ASP.NET Core Web API
- Blazor Server
- Dapper para acesso a dados
- SQLite como banco de dados
- MediatR para implementação do padrão CQRS
- JWT Bearer Authentication
- BCrypt para hash de senhas
- KafkaFlow para mensageria assíncrona
- Docker e Docker Compose

## Como Executar

Para rodar o projeto completo com Docker, execute:

```
docker-compose up --build
```

Isso irá iniciar todos os serviços incluindo Kafka e Zookeeper. As URLs ficam disponíveis em:

- Frontend: http://localhost:5000
- Contas API: http://localhost:5001/swagger
- Transferencias API: http://localhost:5002/swagger
- Tarifas API: http://localhost:5003/swagger

## Rotas da API

### Contas API (http://localhost:5001)

POST /api/contas/cadastrar
Cadastra uma nova conta corrente. Requer CPF, senha e nome. Valida o CPF usando algoritmo completo e gera um número de conta único de 10 dígitos.

Body:
```
{
  "cpf": "12345678901",
  "password": "senha123",
  "name": "João Silva"
}
```

POST /api/contas/entrar
Realiza login e retorna token JWT. Aceita CPF ou número da conta como login.

Body:
```
{
  "login": "12345678901",
  "password": "senha123"
}
```

GET /api/contas/saldo?accountNumber=1234567890
Consulta o saldo da conta. Requer token JWT no header Authorization. Se não informar accountNumber, usa a conta do token.

Headers: Authorization: Bearer {token}

POST /api/contas/movimentacoes
Realiza movimentação de crédito ou débito. Requer token JWT. Créditos podem ser feitos em qualquer conta, débitos apenas na conta logada.

Headers: Authorization: Bearer {token}
Body:
```
{
  "requestId": "unique-id-123",
  "accountNumber": "1234567890",
  "amount": 100.00,
  "type": "C"
}
```

POST /api/contas/inativar
Inativa uma conta. Requer token JWT e confirmação por senha.

Headers: Authorization: Bearer {token}
Body:
```
{
  "password": "senha123"
}
```

### Transferencias API (http://localhost:5002)

POST /api/transferencias
Realiza transferência entre contas da mesma instituição. A conta de origem é obtida automaticamente do token JWT. Em caso de falha no crédito, o débito é estornado automaticamente.

Headers: Authorization: Bearer {token}
Body:
```
{
  "requestId": "unique-id-456",
  "destinationAccountNumber": "0987654321",
  "amount": 50.00
}
```

### Tarifas API (http://localhost:5003)

GET /api/tarifas
Lista todas as tarifas aplicadas no sistema com paginação. Parâmetros opcionais: skip (padrão 0) e take (padrão 100, máximo 100).

GET /api/tarifas/conta/{accountNumber}
Lista todas as tarifas aplicadas em uma conta específica.

GET /api/tarifas/transferencia/{transferId}
Obtém uma tarifa específica pelo ID da transferência.

GET /api/tarifas/existe/{transferId}
Verifica se uma tarifa foi aplicada para uma transferência específica.

## Desenvolvimento

O projeto foi desenvolvido seguindo Domain-Driven Design com separação clara entre camadas. A camada Domain contém as entidades, value objects e interfaces de repositório. A Application implementa os comandos e queries usando MediatR, seguindo o padrão CQRS. A Infrastructure implementa os repositórios usando Dapper, serviços de infraestrutura como hash de senhas e geração de tokens JWT, e integração com Kafka para mensageria assíncrona.

A autenticação utiliza JWT Bearer tokens com validação customizada através de middleware. Todas as operações financeiras são idempotentes através do uso de RequestId. As transferências implementam compensação automática em caso de falha, garantindo consistência dos dados.

A aplicação de tarifas funciona de forma assíncrona. Quando uma transferência é realizada, a Transferencias API publica uma mensagem no Kafka. A Tarifas API consome essa mensagem, aplica a tarifa fixa de R$ 2,00 e publica outra mensagem para a Contas API debitar o valor automaticamente na conta.

Os bancos de dados SQLite são criados automaticamente na primeira execução através de DatabaseInitializer em cada microsserviço. Os dados são persistidos em volumes Docker para manter os dados entre reinicializações.

## Migração para Oracle

Para usar Oracle em produção ao invés de SQLite, são necessárias algumas alterações. A complexidade é baixa pois o código já utiliza Dapper e padrões que facilitam a migração.

### Alterações Necessárias

**1. Pacotes NuGet**

Nos arquivos `.csproj` de cada Infrastructure, substituir:
- Remover: `System.Data.SQLite.Core`
- Adicionar: `Oracle.ManagedDataAccess.Core`

Arquivos a alterar:
- `src/BankMore.Contas.Infrastructure/BankMore.Contas.Infrastructure.csproj`
- `src/BankMore.Transferencias.Infrastructure/BankMore.Transferencias.Infrastructure.csproj`
- `src/BankMore.Tarifas.Infrastructure/BankMore.Tarifas.Infrastructure.csproj`

**2. Connection String**

Atualizar as connection strings nos `appsettings.json` ou variáveis de ambiente. O formato Oracle é diferente:

```
Data Source=//host:port/servicename;User Id=usuario;Password=senha;
```

Ou usando TNS:
```
Data Source=tnsname;User Id=usuario;Password=senha;
```

**3. DependencyInjection**

Em cada `DependencyInjection.cs` da camada Infrastructure, substituir:
- `using System.Data.SQLite;` por `using Oracle.ManagedDataAccess.Client;`
- `new SQLiteConnection(connectionString)` por `new OracleConnection(connectionString)`

Arquivos a alterar:
- `src/BankMore.Contas.Infrastructure/DependencyInjection.cs`
- `src/BankMore.Transferencias.Infrastructure/DependencyInjection.cs`
- `src/BankMore.Tarifas.Infrastructure/DependencyInjection.cs`

**4. Scripts SQL no DatabaseInitializer**

Nos arquivos `DatabaseInitializer.cs` de cada microsserviço, ajustar os scripts SQL:

- `AUTOINCREMENT` deve ser substituído por `GENERATED ALWAYS AS IDENTITY` ou usar sequências
- `TEXT` deve ser `VARCHAR2`
- `INTEGER` pode permanecer como `NUMBER` ou usar `INTEGER`
- `DECIMAL(18,2)` pode permanecer ou usar `NUMBER(18,2)`
- `DATETIME` deve ser `DATE` ou `TIMESTAMP`
- `CREATE TABLE IF NOT EXISTS` não existe no Oracle, usar condicional ou script separado
- Índices podem usar a mesma sintaxe

Exemplo de conversão para a tabela Accounts:

```sql
CREATE TABLE Accounts (
    Id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    Cpf VARCHAR2(11) NOT NULL UNIQUE,
    AccountNumber VARCHAR2(10) NOT NULL UNIQUE,
    Name VARCHAR2(200) NOT NULL,
    PasswordHash VARCHAR2(255) NOT NULL,
    Status NUMBER(1) NOT NULL DEFAULT 1,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP
);

CREATE INDEX IX_Accounts_Cpf ON Accounts(Cpf);
CREATE INDEX IX_Accounts_AccountNumber ON Accounts(AccountNumber);
```

Arquivos a alterar:
- `src/BankMore.Contas.Infrastructure/Database/DatabaseInitializer.cs`
- `src/BankMore.Transferencias.Infrastructure/Database/DatabaseInitializer.cs`
- `src/BankMore.Tarifas.Infrastructure/Database/DatabaseInitializer.cs`

**5. Tratamento de IF NOT EXISTS**

Oracle não suporta `CREATE TABLE IF NOT EXISTS`. Duas opções:

- Criar os scripts manualmente no banco e remover a inicialização automática
- Ou adicionar lógica condicional no código para verificar se a tabela existe antes de criar

**6. Testes**

Validar que as queries do Dapper funcionam corretamente, especialmente:
- Parâmetros nomeados (já funcionam)
- Transações (comportamento pode variar)
- Tipos de dados retornados nas consultas



