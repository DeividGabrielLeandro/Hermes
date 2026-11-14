const formulario = document.getElementById("cadastro");
const erro = document.getElementById("form-erro");

formulario.addEventListener("submit", (evento) => {
  evento.preventDefault();
  const email = document.getElementById("email").value;
  sessionStorage.setItem("email", email);
  sessionStorage.setItem("tipoCodigo", "redefinir_senha");
  window.location.href = "verificar-codigo.html";
});
