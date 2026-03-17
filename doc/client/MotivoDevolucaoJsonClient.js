var OMIE_APP_KEY = 'PUT_YOUR_APP_KEY_HERE';
var OMIE_APP_SECRET = 'PUT_YOUR_APP_SECRET_HERE';

var XMLHttpRequest = require("xmlhttprequest").XMLHttpRequest;
var MotivoDevolucaoJsonClient=function(){
	this._EndPoint="https://app.omie.com.br/api/v1/geral/motivodevolucao/";
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
	this.ListarMotivosDevol=function(
		ListarMotivoDevolRequest,
		_cb
	){
		return this._Call(
			"ListarMotivosDevol",
			[
			ListarMotivoDevolRequest
			],
			(_cb)?_cb:null
		);
	};
	this.listaMotivo=function(){
		this.nCodigo=null;
		this.cDescricao=null;
		this.cInativo=null;
	};
	this.ListarMotivoDevolRequest=function(){
		this.nPagina=null;
		this.nRegPorPagina=null;
	};
	this.ListarMotivoDevolResponse=function(){
		this.nPagina=null;
		this.nTotPaginas=null;
		this.nRegistros=null;
		this.nTotRegistros=null;
		this.listaMotivo=null;
	};
	this.omie_fail=function(){
		this.code=null;
		this.description=null;
		this.referer=null;
		this.fatal=null;
	};
};
module.exports = MotivoDevolucaoJsonClient;