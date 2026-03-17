var OMIE_APP_KEY = 'PUT_YOUR_APP_KEY_HERE';
var OMIE_APP_SECRET = 'PUT_YOUR_APP_SECRET_HERE';

var XMLHttpRequest = require("xmlhttprequest").XMLHttpRequest;
var FormasPagComprasJsonClient=function(){
	this._EndPoint="https://app.omie.com.br/api/v1/produtos/formaspagcompras/";
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
	this.ListarFormasPagCompras=function(
		comparListarRequest,
		_cb
	){
		return this._Call(
			"ListarFormasPagCompras",
			[
			comparListarRequest
			],
			(_cb)?_cb:null
		);
	};
	this.cadastros=function(){
		this.cCodigo=null;
		this.nQtdeParc=null;
		this.cDescricao=null;
		this.cListaParc=null;
		this.nDiasParc=null;
	};
	this.comparListarRequest=function(){
		this.pagina=null;
		this.registros_por_pagina=null;
		this.ordenar_por=null;
		this.ordem_decrescente=null;
	};
	this.comparListarResponse=function(){
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
module.exports = FormasPagComprasJsonClient;