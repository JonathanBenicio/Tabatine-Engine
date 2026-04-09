var OMIE_APP_KEY = 'PUT_YOUR_APP_KEY_HERE';
var OMIE_APP_SECRET = 'PUT_YOUR_APP_SECRET_HERE';

var XMLHttpRequest = require("xmlhttprequest").XMLHttpRequest;
var ResumoEstoqueJsonClient=function(){
	this._EndPoint="https://app.omie.com.br/api/v1/estoque/resumo/";
	this._Call=function(method,param,cb){
		var server= new XMLHttpRequest();
		server.open("POST",this._EndPoint,cb!=null);
		server.setRequestHeader("Content-Type","application/json");
		var req=JSON.stringify({call:method,app_key:OMIE_APP_KEY,app_secret:OMIE_APP_SECRET,param:(param)?param:[]});
		if(cb){
			server.onreadystatechange=this._EndCall;
			server.cb=cb;
			server.send(req);
			return server;
		}else{
			server.send(req);
			var res=JSON.parse(server.responseText);
			delete(server);
			return res;
		}
	};
	this._EndCall=function(e){
		var server=this;
		if(server.readyState!=4)
			return;
		if(server.status!=200)
			throw(new Exception("AJAX error "+server.status+": "+server.statusText));
		server.cb(JSON.parse(server.responseText));
		server.cb=null;
		delete(server);
	};
	this.ObterEstoqueProduto=function(
		ObterEstoqueProdRequest,
		_cb
	){
		return this._Call(
			"ObterEstoqueProduto",
			[
			ObterEstoqueProdRequest
			],
			(_cb)?_cb:null
		);
	};
	this.listaEstoque=function(){
		this.nIdlocal=null;
		this.nFisico=null;
		this.nReservado=null;
		this.nPrevisaoSaida=null;
		this.nPrevisaoEntrada=null;
		this.nDisponivel=null;
		this.nCMC=null;
		this.nPrecoUnitario=null;
		this.nPrecoUltComp=null;
		this.dDtUltComp=null;
		this.nEstoqueMinimo=null;
		this.cIcone=null;
		this.cCor=null;
		this.cDescricaoLocal=null;
	};
	this.listaImagens=function(){
		this.cUrlImagem=null;
	};
	this.listaProduto=function(){
		this.nIdProduto=null;
		this.cCodigo=null;
		this.cDescricao=null;
		this.cEAN=null;
		this.cNCM=null;
		this.cUnidade=null;
	};
	this.ObterEstoqueProdRequest=function(){
		this.cEAN=null;
		this.nIdProduto=null;
		this.cCodigo=null;
		this.xCodigo=null;
		this.dDia=null;
		this.cExibirImagens=null;
	};
	this.ObterEstoqueProdResponse=function(){
		this.nIdProduto=null;
		this.cCodigo=null;
		this.cDescricao=null;
		this.cEAN=null;
		this.dDia=null;
		this.cNCM=null;
		this.cUnidade=null;
		this.listaEstoque=null;
		this.listaProduto=null;
		this.listaImagens=null;
	};
	this.omie_fail=function(){
		this.code=null;
		this.description=null;
		this.referer=null;
		this.fatal=null;
	};
};
module.exports = ResumoEstoqueJsonClient;