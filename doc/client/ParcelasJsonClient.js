var OMIE_APP_KEY = 'PUT_YOUR_APP_KEY_HERE';
var OMIE_APP_SECRET = 'PUT_YOUR_APP_SECRET_HERE';

var XMLHttpRequest = require("xmlhttprequest").XMLHttpRequest;
var ParcelasJsonClient=function(){
	this._EndPoint="https://app.omie.com.br/api/v1/geral/parcelas/";
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
	this.ListarParcelas=function(
		parcelaListarRequest,
		_cb
	){
		return this._Call(
			"ListarParcelas",
			[
			parcelaListarRequest
			],
			(_cb)?_cb:null
		);
	};
	this.IncluirParcela=function(
		parcelaIncluirRequest,
		_cb
	){
		return this._Call(
			"IncluirParcela",
			[
			parcelaIncluirRequest
			],
			(_cb)?_cb:null
		);
	};
	this.cadastros=function(){
		this.nCodigo=null;
		this.cDescricao=null;
		this.nParcelas=null;
	};
	this.parcelaIncluirRequest=function(){
		this.cParcela=null;
	};
	this.parcelaIncluirResponse=function(){
		this.cCodStatus=null;
		this.cDesStatus=null;
		this.cCodParcela=null;
		this.cDesParcela=null;
	};
	this.parcelaListarRequest=function(){
		this.pagina=null;
		this.registros_por_pagina=null;
		this.apenas_importado_api=null;
		this.ordenar_por=null;
		this.ordem_decrescente=null;
	};
	this.parcelaListarResponse=function(){
		this.pagina=null;
		this.total_de_paginas=null;
		this.registros=null;
		this.total_de_registros=null;
		this.cadastros=null;
	};
	this.omie_fail=function(){
		this.code=null;
		this.description=null;
		this.referer=null;
		this.fatal=null;
	};
};
module.exports = ParcelasJsonClient;