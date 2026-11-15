const enviar = document.getElementById("enviar");
const mensagem = document.getElementById("mensagem");
const painel = document.getElementById("mensagens");
const email = sessionStorage.getItem("email");
let idConversa = null;

function mostrarMensagem(texto, classe) {
  const Mensagem = document.createElement("p");
  Mensagem.innerHTML = DOMPurify.sanitize(marked.parse(texto));
  Mensagem.className = classe;
  painel.appendChild(Mensagem);
  painel.scrollTop = painel.scrollHeight;
}

async function carregarHistorico(email) {
  const usuario = {
    email: email,
  };

  const jsonString = JSON.stringify(usuario, null, 2);

  const resposta = await fetch("http://localhost:5273/api/conversas", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: jsonString,
  });
  if (resposta.status === 200) {
    const lista = await resposta.json();
    const historico = document.getElementById("historico");

    historico.innerHTML = "";
    for (const item of lista) {
      const li = document.createElement("li");
      const botao = document.createElement("button");
      botao.className = "conversa";
      botao.type = "button";
      botao.textContent = item.titulo;

      botao.addEventListener("click", () => {
        console.log(item.id);
      });

      li.appendChild(botao);
      historico.appendChild(li);
    }
  }
}
carregarHistorico(email);

async function enviarJSON(mensagem, email) {
  const usuario = {
    mensagem: mensagem,
    email: email,
    idConversa: idConversa,
  };

  const jsonString = JSON.stringify(usuario, null, 2);

  const resposta = await fetch("http://localhost:5273/api/auth/chat-bot", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: jsonString,
  });
  if (resposta.status === 200) {
    const dados = await resposta.json();
    idConversa = dados.conversaId;
    mostrarMensagem(dados.resposta, "mensagemAPI");
    carregarHistorico(email);
  }
}
mensagem.addEventListener("keydown", (evento) => {
  if (evento.key === "Enter" && !evento.shiftKey) {
    evento.preventDefault();
    const texto = mensagem.value.trim();
    if (!texto) return;
    mensagem.value = "";
    mostrarMensagem(texto, "mensagemUsuario");
    enviarJSON(texto, email);
  }
});
enviar.addEventListener("click", (evento) => {
  evento.preventDefault();
  const texto = mensagem.value.trim();
  if (!texto) return;
  mensagem.value = "";
  mostrarMensagem(texto, "mensagemUsuario");
  enviarJSON(texto, email);
});
