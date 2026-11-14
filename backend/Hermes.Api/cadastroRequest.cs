using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public record CadastroRequest(string Nome, string Email, string Senha);

public record LoginRequest(string Email, string Senha);
public record VerificarCodigo(string Email, string Codigo, string tipo);
public record RedefinirSenha(string Codigo, string Email, string Senha,  string tipo);


