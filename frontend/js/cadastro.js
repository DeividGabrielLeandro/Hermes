
const erro = document.getElementById("form-erro")
const formulario = document.getElementById("cadastro")

function senhaConfirmada(senha, senha_confirmada){

    if(senha === senha_confirmada){
        erro.textContent = "";
        return true;
    }
    else{
        erro.textContent = "As senhas digitadas estão diferentes"
        return false;
    }
}

async function enviarJSON(nome, email, senha){
    const usuario = {
        nome: nome,
        email: email,
        senha: senha
    }

const jsonString = JSON.stringify(usuario, null, 2);

const resposta = await fetch("http://localhost:5273/api/usuarios", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: jsonString
});
if(resposta.status === 409){
    erro.textContent = "Esse Email já está em uso, tente outro.";

}

console.log(resposta.status);

}

formulario.addEventListener("submit", (evento) => {
    evento.preventDefault();

const nome = document.getElementById("nome").value
const email = document.getElementById("email").value
const senha = document.getElementById("senha").value
const senha_confirmada = document.getElementById("confirmar-senha").value

if(senhaConfirmada(senha,senha_confirmada)){
    enviarJSON(nome, email, senha);
}


});


