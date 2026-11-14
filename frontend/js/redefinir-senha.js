const erro = document.getElementById("form-erro");
const formulario = document.getElementById("cadastro");
const email = sessionStorage.getItem("email");
const tipo = sessionStorage.getItem("tipoCodigo");
const codigo = sessionStorage.getItem("codigo");

function senhaConfirmada(senha, senha_confirmada) {
  if (senha === senha_confirmada) {
    erro.textContent = "";
    return true;
  } else {
    erro.textContent = "As senhas digitadas estão diferentes";
    return false;
  }
}

async function enviarJSON(codigo, email, senha, tipo) {
  const usuario = {
    codigo: codigo,
    email: email,
    senha: senha,
    tipo: tipo,
  };

  const jsonString = JSON.stringify(usuario, null, 2);

  const resposta = await fetch("http://localhost:5273/api/auth/redefinir-senha", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: jsonString,
  });
  if (resposta.status === 401) {
    erro.textContent = "Código inválido ou expirado.";
  } else if (resposta.status === 400) {
    erro.textContent = "Senha com menos de 8 caracteres";
  } else if (resposta.status === 200) {
    sessionStorage.removeItem("codigo");
    window.location.href = "login.html";
  }
}

formulario.addEventListener("submit", (evento) => {
  evento.preventDefault();

  const senha = document.getElementById("senha").value;
  const senha_confirmada = document.getElementById("confirmar-senha").value;

  if (senhaConfirmada(senha, senha_confirmada)) {
    enviarJSON(codigo, email, senha, tipo);
  }
});
