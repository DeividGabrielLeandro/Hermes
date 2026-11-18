const enviar = document.getElementById("enviar");
const barraLateral = document.getElementById("ocultarBarraLateral");
const novaConversa = document.getElementById("novaConversa");
const mensagem = document.getElementById("mensagem");
const painel = document.getElementById("mensagens");
const busca = document.getElementById("buscarConversas");
const sairJanelaAPI = document.getElementById("cancelarChave");
const entrarJanelaAPI = document.getElementById("botaoChave");
const janelaChave = document.getElementById("janelaChave");
const salvarChave = document.getElementById("salvarChave");
const campoChave = document.getElementById("novaChave");
const principal = document.querySelector(".Principal");
const titulo = document.getElementById("tituloConversa");
const email = sessionStorage.getItem("email");
let idConversa = null;
let nomeUsuario = null;



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

  const resposta = await fetch(API_URL + "/api/conversas", {
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

      if (item.id === idConversa) {
        titulo.textContent = item.titulo;
        botao.classList.add("ativa");
      }

      botao.addEventListener("click", () => {
        titulo.textContent = item.titulo;
        tirarDestaque();
        botao.classList.add("ativa");
        carregarConversa(item.id, email);
      });

      li.appendChild(botao);
      historico.appendChild(li);
    }
  }
}

carregarHistorico(email);

async function carregarConversa(id, email) {
  const usuario = {
    email: email,
    idConversa: id,
  };
  idConversa = id;

  const jsonString = JSON.stringify(usuario, null, 2);

  const resposta = await fetch(API_URL + "/api/conversas/abrir", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: jsonString,
  });
  if (resposta.status === 200) {
    const lista = await resposta.json();
    painel.innerHTML = "";
    for (const item of lista) {
      const classe = item.papel === "user" ? "mensagemUsuario" : "mensagemAPI";
      mostrarMensagem(item.conteudo, classe);
    }
  }
}
async function enviarJSON(mensagem, email) {
  const usuario = {
    mensagem: mensagem,
    email: email,
    idConversa: idConversa,
  };

  const jsonString = JSON.stringify(usuario, null, 2);

  const resposta = await fetch(API_URL + "/api/auth/chat-bot", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: jsonString,
  });
  if (resposta.status === 200) {
    const dados = await resposta.json();
    idConversa = dados.conversaId;
    mostrarMensagem(dados.resposta, "mensagemAPI");
    carregarHistorico(email);
  } else if (resposta.status === 400) {
    mostrarMensagem(await resposta.json(), "mensagemAPI");
  }
}
async function enviarChave(chaveAPI, email) {
  const usuario = {
    email: email,
    apiKey: chaveAPI,
  };

  const jsonString = JSON.stringify(usuario, null, 2);

  const resposta = await fetch(API_URL + "/api/chaves", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: jsonString,
  });
  if (resposta.status === 200) {
    const dados = await resposta.json();
    document.getElementById("finalChave").textContent = "••••" + dados.finalChave;
    document.getElementById("status").textContent = "Configurada";
    campoChave.value = "";
    janelaChave.close();
  }
}
async function buscarNome(email) {
  const usuario = {
    email: email,
  };

  const jsonString = JSON.stringify(usuario, null, 2);

  const resposta = await fetch(API_URL + "/api/usuario/nome", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: jsonString,
  });
  if (resposta.status === 200) {
    const dados = await resposta.json();
    nomeUsuario = dados.nome;
    document.getElementById("nomeUsuario").textContent = nomeUsuario;
    document.querySelector(".perfil .avatar").textContent = nomeUsuario[0].toUpperCase();
  }
}
buscarNome(email);

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

novaConversa.addEventListener("click", (evento) => {
  evento.preventDefault();
  idConversa = null;
  painel.innerHTML = "";
  titulo.textContent = "";
  tirarDestaque();
});

// Tira o destaque de todas as conversas da lista
function tirarDestaque() {
  for (const botao of document.querySelectorAll("#historico .conversa")) {
    botao.classList.remove("ativa");
  }
}

// Sair: esquece o e-mail desta aba e volta pro login
document.getElementById("sair").addEventListener("click", () => {
  sessionStorage.clear();
  window.location.href = "login.html";
});

busca.addEventListener("input", () => {
  const termo = busca.value.toLowerCase();
  const itens = document.querySelectorAll("#historico li");

  for (const li of itens) {
    const titulo = li.textContent.toLowerCase();
    li.hidden = !titulo.includes(termo);
  }
});

entrarJanelaAPI.addEventListener("click", (evento) => {
  evento.preventDefault();
  janelaChave.showModal();
  carregarHistorico(email);
});
sairJanelaAPI.addEventListener("click", (evento) => {
  evento.preventDefault();
  janelaChave.close();
  carregarHistorico(email);
});

salvarChave.addEventListener("click", () => {
  const chave = campoChave.value.trim();
  if (!chave) return;
  enviarChave(chave, email);
});

barraLateral.addEventListener("click", () => {
  principal.classList.toggle("lateral-fechada");
});
