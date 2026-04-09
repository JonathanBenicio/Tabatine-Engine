var OMIE_APP_KEY = 'PUT_YOUR_APP_KEY_HERE';
var OMIE_APP_SECRET = 'PUT_YOUR_APP_SECRET_HERE';

var XMLHttpRequest = require("xmlhttprequest").XMLHttpRequest;
var MovimentoEstoqueJsonClient=function(){
	this._EndPoint="https://app.omie.com.br/api/v1/estoque/movestoque/";
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
	this.ListarMovimentos=function(
		epListarRequest,
		_cb
	){
		return this._Call(
			"ListarMovimentos",
			[
			epListarRequest
			],
			(_cb)?_cb:null
		);
	};
	this.ConsultarPrevisao=function(
		epPrevisaoRequest,
		_cb
	){
		return this._Call(
			"ConsultarPrevisao",
			[
			epPrevisaoRequest
			],
			(_cb)?_cb:null
		);
	};
	this.cadastros=function(){
		this.nCodProd=null;
		this.cCodIntProd=null;
		this.cCodigo=null;
		this.cDescricao=null;
		this.movimentos=null;
	};
	this.movimentos=function(){
		this.dDataMovimento=null;
		this.nQtdeEntradas=null;
		this.nQtdeSaidas=null;
	};
	this.epListarRequest=function(){
		this.pagina=null;
		this.registros_por_pagina=null;
		this.apenas_importado_api=null;
		this.ordenar_por=null;
		this.ordem_decrescente=null;
		this.data_inicial=null;
		this.data_final=null;
		this.hora_inicial=null;
		this.hora_final=null;
		this.codigo_local_estoque=null;
	};
	this.epListarResponse=function(){
		this.pagina=null;
		this.total_de_paginas=null;
		this.registros=null;
		this.total_de_registros=null;
		this.cadastros=null;
	};
	this.epPrevisaoRequest=function(){
		this.nCodProd=null;
		this.cCodIntProd=null;
		this.cCodigo=null;
		this.dDtInicial=null;
		this.dDtFinal=null;
		this.codigo_local_estoque=null;
	};
	this.epPrevisaoResponse=function(){
		this.nCodProd=null;
		this.cCodIntProd=null;
		this.cCodigo=null;
		this.cDescricao=null;
		this.nQtdePrevista=null;
		this.codigo_local_estoque=null;
	};
	this.omie_fail=function(){
		this.code=null;
		this.description=null;
		this.referer=null;
		this.fatal=null;
	};
};
module.exports = MovimentoEstoqueJsonClient;