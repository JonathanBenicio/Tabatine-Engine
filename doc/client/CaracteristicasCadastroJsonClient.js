var OMIE_APP_KEY = 'PUT_YOUR_APP_KEY_HERE';
var OMIE_APP_SECRET = 'PUT_YOUR_APP_SECRET_HERE';

var XMLHttpRequest = require("xmlhttprequest").XMLHttpRequest;
var CaracteristicasCadastroJsonClient=function(){
	this._EndPoint="https://app.omie.com.br/api/v1/geral/caracteristicas/";
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
	this.IncluirCaracteristica=function(
		caractIncluirRequest,
		_cb
	){
		return this._Call(
			"IncluirCaracteristica",
			[
			caractIncluirRequest
			],
			(_cb)?_cb:null
		);
	};
	this.AlterarCaracteristica=function(
		caractAlterarRequest,
		_cb
	){
		return this._Call(
			"AlterarCaracteristica",
			[
			caractAlterarRequest
			],
			(_cb)?_cb:null
		);
	};
	this.ExcluirCaracteristica=function(
		caractExcluirRequest,
		_cb
	){
		return this._Call(
			"ExcluirCaracteristica",
			[
			caractExcluirRequest
			],
			(_cb)?_cb:null
		);
	};
	this.ConsultarCaracteristica=function(
		caractConsultarRequest,
		_cb
	){
		return this._Call(
			"ConsultarCaracteristica",
			[
			caractConsultarRequest
			],
			(_cb)?_cb:null
		);
	};
	this.ListarCaracteristicas=function(
		caractListarRequest,
		_cb
	){
		return this._Call(
			"ListarCaracteristicas",
			[
			caractListarRequest
			],
			(_cb)?_cb:null
		);
	};
	this.caractAlterarRequest=function(){
		this.nCodCaract=null;
		this.cCodIntCaract=null;
		this.cNomeCaract=null;
		this.cValorDef=null;
		this.conteudosPermitidos=null;
	};
	this.conteudosPermitidos=function(){
		this.cConteudo=null;
		this.nIdConteudo=null;
	};
	this.caractAlterarResponse=function(){
		this.nCodCaract=null;
		this.cCodIntCaract=null;
		this.cCodStatus=null;
		this.cDesStatus=null;
	};
	this.caractConsultarRequest=function(){
		this.nCodCaract=null;
		this.cCodIntCaract=null;
	};
	this.caractConsultarResponse=function(){
		this.nCodCaract=null;
		this.cCodIntCaract=null;
		this.cNomeCaract=null;
		this.conteudosPermitidos=null;
		this.info=null;
	};
	this.info=function(){
		this.dInc=null;
		this.hInc=null;
		this.uInc=null;
		this.dAlt=null;
		this.hAlt=null;
		this.uAlt=null;
		this.cImpAPI=null;
	};
	this.caractExcluirRequest=function(){
		this.nCodCaract=null;
		this.cCodIntCaract=null;
	};
	this.caractExcluirResponse=function(){
		this.nCodCaract=null;
		this.cCodIntCaract=null;
		this.cCodStatus=null;
		this.cDesStatus=null;
	};
	this.caractIncluirRequest=function(){
		this.cCodIntCaract=null;
		this.cNomeCaract=null;
		this.cValorDef=null;
		this.conteudosPermitidos=null;
	};
	this.caractIncluirResponse=function(){
		this.nCodCaract=null;
		this.cCodIntCaract=null;
		this.cCodStatus=null;
		this.cDesStatus=null;
	};
	this.caractListarRequest=function(){
		this.nPagina=null;
		this.nRegPorPagina=null;
		this.cOrdenarPor=null;
		this.cOrdemDecrescente=null;
		this.dDtIncDe=null;
		this.dDtIncAte=null;
		this.dDtAltDe=null;
		this.dDtAltAte=null;
	};
	this.caractListarResponse=function(){
		this.nPagina=null;
		this.nTotPaginas=null;
		this.nRegistros=null;
		this.nTotRegistros=null;
		this.listaCaracteristicas=null;
	};
	this.listaCaracteristicas=function(){
		this.nCodCaract=null;
		this.cCodIntCaract=null;
		this.cNomeCaract=null;
		this.conteudosPermitidos=null;
		this.info=null;
	};
	this.omie_fail=function(){
		this.code=null;
		this.description=null;
		this.referer=null;
		this.fatal=null;
	};
};
module.exports = CaracteristicasCadastroJsonClient;