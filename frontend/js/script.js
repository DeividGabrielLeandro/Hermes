const botaoTema = document.getElementById("tema_pagina");
const html = document.documentElement;

const temaSalvo = localStorage.getItem("tema")
if(temaSalvo){
    html.dataset.tema = temaSalvo
}

function temaAtual() {
  if (html.dataset.tema) {
    return html.dataset.tema;
  }
  if (window.matchMedia("(prefers-color-scheme: light)").matches) {
    return "claro";
  }
  return "escuro";
}

botaoTema.addEventListener("click", () => 
    {
        const novoTema = temaAtual() === "escuro" ? "claro": "escuro";
        html.dataset.tema = novoTema;
        localStorage.setItem("tema",novoTema);
    })
