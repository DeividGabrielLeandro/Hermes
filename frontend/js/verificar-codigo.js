const email = sessionStorage.getItem("email");
const tipo = sessionStorage.getItem("tipoCodigo");
const formulario = document.getElementById("cadastro");
const erro = document.getElementById("form-erro");
document.getElementById("email-enviado").textContent = email;
EnviarCodigo(email, tipo);

async function EnviarCodigo(email, tipo) {
  const usuario = { email: email, tipo: tipo };

  const jsonString = JSON.stringify(usuario, null, 2);

  const resposta = await fetch("http://localhost:5273/api/auth/enviar-codigo", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: jsonString,
  });
}
async function ConfirmarCodigo(codigo, email, tipo ) {
  const usuario = { email: email, codigo: codigo, tipo: tipo};

  const jsonString = JSON.stringify(usuario, null, 2);

  const resposta = await fetch("http://localhost:5273/api/auth/verificar-codigo", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: jsonString,
  });

  if (resposta.status == 401) {
    erro.textContent = "Código incorreto ou expirado";
  } else if (resposta.status == 200) {
    if (tipo === "redefinir_senha") {
      sessionStorage.setItem("email", email);
      sessionStorage.setItem("codigo", codigo)
      window.location.href = "redefinir-senha.html";
    } else if (tipo === "verificar_email") {
      window.location.href = "chat.html";
      alert("Cadastro realizado com sucesso!");
    }
  }
}

formulario.addEventListener("submit", (evento) => {
  evento.preventDefault();
  const codigo = document.getElementById("codigo").value;
  ConfirmarCodigo(codigo, email, tipo);
});
