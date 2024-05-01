# PulsePay

Um sistema intermediador de APIs de pagamento que utiliza o padrão de design Domain-Driven Design (DDD) para facilitar a integração com várias APIs de pagamento usando C# e PostgreSQL.

## Funcionalidades

- **Cadastro de Usuários**: Permite cadastrar usuários no sistema.
- **Gestão de Pagamentos**: Intermediação de pagamentos com diferentes gateways.
- **Logs de Transações**: Registro detalhado de todas as transações processadas.

## Tecnologias Utilizadas

- **C#**
- **.NET 8**
- **Entity Framework Core**
- **PostgreSQL**
- **Swagger** para documentação da API

## Configuração do Projeto

Este projeto usa o Entity Framework Core para a persistência de dados e o PostgreSQL como sistema de gerenciamento de banco de dados.

### Pré-requisitos

- .NET 8 SDK
- PostgreSQL
- Um editor de código ou IDE compatível com projetos .NET, como Visual Studio ou VS Code.

### Instalação

1. **Clone o repositório**
   ```bash
   git clone https://github.com/Victor-cmda/PulsePay.git
   cd PulsePay
   ```

2. **Configure a string de conexão**
   - Modifique a string de conexão no arquivo `appsettings.json` ou `appsettings.Development.json` para apontar para seu servidor PostgreSQL.

3. **Instale as dependências do projeto**
   ```bash
   dotnet restore
   ```

4. **Execute as migrações para criar o banco de dados**
   ```bash
   dotnet ef database update
   ```

### Execução

- Para rodar o projeto:
  ```bash
  dotnet run
  ```

- Acesse `http://localhost:7000/swagger` para ver a interface do Swagger e testar os endpoints da API.

## Como Contribuir

1. **Crie sua Feature Branch** (`git checkout -b feature/NovaFeature`)
2. **Faça commit de suas mudanças** (`git commit -am 'Adicionar alguma feature'`)
3. **Push para a Branch** (`git push origin feature/NovaFeature`)
4. **Abra um Pull Request**
