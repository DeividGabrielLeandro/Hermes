const formulario = document.getElementById("login");
const erro = document.getElementById("form-erro");

erro.textContent = "";

async function enviarJSON(email, senha) {
  const usuario = {
    email: email,
    senha: senha,
  };

  const jsonString = JSON.stringify(usuario, null, 2);

  const resposta = await fetch(API_URL + "/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: jsonString,
  });

  if (resposta.status === 401) {
    erro.textContent = "Email ou senha incorretos, tente novamente.";
  }
  else if (resposta.status === 200) {
    const dados = await resposta.json()
    sessionStorage.setItem("token", dados.token)
    sessionStorage.setItem("email", email);
    window.location.href = "chat.html";
  }
}
formulario.addEventListener("submit", (evento) => {
  evento.preventDefault();

  const email = document.getElementById("email").value;
  const senha = document.getElementById("senha").value;

  enviarJSON(email, senha);
});
