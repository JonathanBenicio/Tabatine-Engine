var OMIE_APP_KEY = 'PUT_YOUR_APP_KEY_HERE';
var OMIE_APP_SECRET = 'PUT_YOUR_APP_SECRET_HERE';

var XMLHttpRequest = require("xmlhttprequest").XMLHttpRequest;
var EtapasFaturamentoJsonClient=function(){
	this._EndPoint="https://app.omie.com.br/api/v1/produtos/etapafat/";
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
	this.ListarEtapasFaturamento=function(
		etaproListarRequest,
		_cb
	){
		return this._Call(
			"ListarEtapasFaturamento",
			[
			etaproListarRequest
			],
			(_cb)?_cb:null
		);
	};
	this.cadastros=function(){
		this.cCodOperacao=null;
		this.cDescOperacao=null;
		this.etapas=null;
	};
	this.etapas=function(){
		this.cCodigo=null;
		this.cDescricao=null;
		this.cDescrPadrao=null;
		this.cInativo=null;
	};
	this.etaproListarRequest=function(){
		this.pagina=null;
		this.registros_por_pagina=null;
		this.ordenar_por=null;
		this.ordem_decrescente=null;
	};
	this.etaproListarResponse=function(){
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
module.exports = EtapasFaturamentoJsonClient;