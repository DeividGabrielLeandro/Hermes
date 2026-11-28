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
const mensagemApi = document.getElementById("mensagemApiSalva");
const principal = document.querySelector(".Principal");
const titulo = document.getElementById("tituloConversa");
const listaModelos = document.getElementById("selecionarModelo");
const listaChaves = document.getElementById("listaChaves");
const email = sessionStorage.getItem("email");
const token = sessionStorage.getItem("token");
let idConversa = null;
let nomeUsuario = null;

function sessaoExpirou() {
  sessionStorage.clear();
  window.location.href = "login.html";
}

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
    headers: {
      "Content-Type": "application/json",
      Authorization: "Bearer " + token,
    },
    body: jsonString,
  });

  if (resposta.status === 200) {
    const lista = await resposta.json();
    const historico = document.getElementById("historico");

    historico.innerHTML = "";
    const pastas = {};
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
        document.getElementById("provedorApi").textContent = item.modelo;
        tirarDestaque();
        botao.classList.add("ativa");
        carregarConversa(item.id, email);
      });

      li.appendChild(botao);
      const nomePasta = item.provedor || "Outros";

      // se a pasta ainda não existe, cria agora
      if (!pastas[nomePasta]) {
        const liPasta = document.createElement("li");
        liPasta.className = "pasta";

        const details = document.createElement("details");
        details.open = true;

        const summary = document.createElement("summary");
        summary.textContent = nomePasta;

        const ul = document.createElement("ul");

        details.appendChild(summary);
        details.appendChild(ul);
        liPasta.appendChild(details);
        historico.appendChild(liPasta);

        pastas[nomePasta] = ul; // anota no caderninho
      }

      // coloca a conversa dentro da pasta certa
      pastas[nomePasta].appendChild(li);
    }
  }
  if (resposta.status === 401) return sessaoExpirou();
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
    headers: {
      "Content-Type": "application/json",
      Authorization: "Bearer " + token,
    },
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
  if (resposta.status === 401) return sessaoExpirou();
}
async function enviarJSON(mensagem, email, modelo) {
  if (enviar.disabled) return;
  enviar.disabled = true;
  const conversaDeOrigem = idConversa;

  const usuario = {
    mensagem: mensagem,
    email: email,
    idConversa: idConversa,
    modelo: modelo,
    provedor: listaModelos.selectedOptions[0].parentElement.label,
  };

  const jsonString = JSON.stringify(usuario, null, 2);

  const resposta = await fetch(API_URL + "/api/auth/chat-bot", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: "Bearer " + token,
    },
    body: jsonString,
  });
  enviar.disabled = false;

  if (resposta.status === 200) {
    const dados = await resposta.json();
    if (idConversa === conversaDeOrigem) {
      idConversa = dados.conversaId;
      mostrarMensagem(dados.resposta, "mensagemAPI");
    }
    carregarHistorico(email);
  } else if (resposta.status === 400) {
    mostrarMensagem(await resposta.json(), "mensagemAPI");
  }
  if (resposta.status === 401) return sessaoExpirou();
}

async function enviarChave(chaveAPI, email) {
  const usuario = {
    email: email,
    apiKey: chaveAPI,
  };

  const jsonString = JSON.stringify(usuario, null, 2);

  const resposta = await fetch(API_URL + "/api/chaves", {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: "Bearer " + token },
    body: jsonString,
  });
  if (resposta.status === 200) {
    const dados = await resposta.json();
    document.getElementById("status").textContent = "Configurada";
    campoChave.value = "";
    janelaChave.close();
    verModelos();
  } else if (resposta.status === 400) {
    ((mensagemApi.textContent = await resposta.json()), "mensagemAPI");
  }
  if (resposta.status === 401) return sessaoExpirou();
}

async function verModelos() {
  const resposta = await fetch(API_URL + "/api/modelos", {
    method: "GET",
    headers: { Authorization: "Bearer " + token },
  });
  if (resposta.status === 200) {
    const dados = await resposta.json();
    listaModelos.innerHTML = "";
    listaChaves.innerHTML = "";

    for (const provedor of dados.provedores) {
      const grupo = document.createElement("optgroup");
      grupo.label = provedor.nome;

      for (const modelo of provedor.modelos) {
        const opcao = document.createElement("option");
        opcao.value = modelo;
        opcao.textContent = modelo;
        grupo.appendChild(opcao);
      }
      const listaChave = document.createElement("li");
      const strong = document.createElement("strong");
      const span = document.createElement("span");

      strong.textContent = provedor.nome;
      span.textContent = provedor.modelos.length + " modelos";

      listaChave.appendChild(strong);
      listaChave.appendChild(span);
      listaChaves.appendChild(listaChave);

      listaModelos.appendChild(grupo);
    }
  } else if (resposta.status === 400) {
    ((mensagemApi.textContent = await resposta.json()), "mensagemAPI");
  }
  if (resposta.status === 401) return sessaoExpirou();
}

verModelos();

async function buscarNome(email) {
  const usuario = {
    email: email,
  };

  const resposta = await fetch(API_URL + "/api/usuario/nome", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: "Bearer " + token,
    },
  });
  if (resposta.status === 200) {
    const dados = await resposta.json();
    nomeUsuario = dados.nome;
    document.getElementById("nomeUsuario").textContent = nomeUsuario;
    document.querySelector(".perfil .avatar").textContent = nomeUsuario[0].toUpperCase();
  }
  if (resposta.status === 401) return sessaoExpirou();
}
buscarNome(email);

mensagem.addEventListener("keydown", (evento) => {
  if (evento.key === "Enter" && !evento.shiftKey) {
    evento.preventDefault();
    const texto = mensagem.value.trim();
    if (!texto) return;
    mensagem.value = "";

    mostrarMensagem(texto, "mensagemUsuario");
    enviarJSON(texto, email, listaModelos.value);
  }
});

enviar.addEventListener("click", (evento) => {
  evento.preventDefault();
  const texto = mensagem.value.trim();
  if (!texto) return;
  mensagem.value = "";
  mostrarMensagem(texto, "mensagemUsuario");
  enviarJSON(texto, email, listaModelos.value);
});

novaConversa.addEventListener("click", (evento) => {
  evento.preventDefault();
  idConversa = null;
  painel.innerHTML = "";
  titulo.textContent = "";
  tirarDestaque();
});

function tirarDestaque() {
  for (const botao of document.querySelectorAll("#historico .conversa")) {
    botao.classList.remove("ativa");
  }
}

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
  if (!chave) {
    mensagemApi.textContent = "Chave de API inválida ou nula.";
    return;
  }

  mensagemApi.textContent = "Chave salva com sucesso.";
  enviarChave(chave, email);
});

barraLateral.addEventListener("click", () => {
  principal.classList.toggle("lateral-fechada");
});
