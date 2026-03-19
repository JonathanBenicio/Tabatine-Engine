var OMIE_APP_KEY = 'PUT_YOUR_APP_KEY_HERE';
var OMIE_APP_SECRET = 'PUT_YOUR_APP_SECRET_HERE';

var XMLHttpRequest = require("xmlhttprequest").XMLHttpRequest;
var PedidoVendaFaturamentoJsonClient=function(){
	this._EndPoint="https://app.omie.com.br/api/v1/produtos/pedidovendafat/";
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
	this.FaturarPedidoVenda=function(
		pvpFaturarRequest,
		_cb
	){
		return this._Call(
			"FaturarPedidoVenda",
			[
			pvpFaturarRequest
			],
			(_cb)?_cb:null
		);
	};
	this.ValidarPedidoVenda=function(
		pvpValidarRequest,
		_cb
	){
		return this._Call(
			"ValidarPedidoVenda",
			[
			pvpValidarRequest
			],
			(_cb)?_cb:null
		);
	};
	this.CancelarPedidoVenda=function(
		pvpCancelarRequest,
		_cb
	){
		return this._Call(
			"CancelarPedidoVenda",
			[
			pvpCancelarRequest
			],
			(_cb)?_cb:null
		);
	};
	this.DuplicarPedidoVenda=function(
		pvpDuplicarRequest,
		_cb
	){
		return this._Call(
			"DuplicarPedidoVenda",
			[
			pvpDuplicarRequest
			],
			(_cb)?_cb:null
		);
	};
	this.AssociarCodIntPedidoVenda=function(
		pvpAssociarCodIntRequest,
		_cb
	){
		return this._Call(
			"AssociarCodIntPedidoVenda",
			[
			pvpAssociarCodIntRequest
			],
			(_cb)?_cb:null
		);
	};
	this.ObterPedidosVenda=function(
		pvpObterRequest,
		_cb
	){
		return this._Call(
			"ObterPedidosVenda",
			[
			pvpObterRequest
			],
			(_cb)?_cb:null
		);
	};
	this.listaPedidosVenda=function(){
		this.nIdPed=null;
		this.cNumPedido=null;
	};
	this.pvpAssociarCodIntRequest=function(){
		this.cCodIntPed=null;
		this.nCodPed=null;
	};
	this.pvpAssociarCodIntResponse=function(){
		this.cCodIntPed=null;
		this.nCodPed=null;
		this.cCodStatus=null;
		this.cDescStatus=null;
	};
	this.pvpCancelarRequest=function(){
		this.cCodIntPed=null;
		this.nCodPed=null;
	};
	this.pvpCancelarResponse=function(){
		this.cCodIntPed=null;
		this.nCodPed=null;
		this.cCodStatus=null;
		this.cDescStatus=null;
	};
	this.pvpDuplicarRequest=function(){
		this.cCodIntPed=null;
		this.nCodPed=null;
	};
	this.pvpDuplicarResponse=function(){
		this.cCodIntPed=null;
		this.nCodPed=null;
		this.cCodStatus=null;
		this.cDescStatus=null;
	};
	this.pvpFaturarRequest=function(){
		this.cCodIntPed=null;
		this.nCodPed=null;
	};
	this.pvpFaturarResponse=function(){
		this.cCodIntPed=null;
		this.nCodPed=null;
		this.cCodStatus=null;
		this.cDescStatus=null;
	};
	this.pvpObterRequest=function(){
		this.cEtapa=null;
	};
	this.pvpObterResponse=function(){
		this.cEtapa=null;
		this.listaPedidosVenda=null;
	};
	this.pvpValidarRequest=function(){
		this.cCodIntPed=null;
		this.nCodPed=null;
	};
	this.pvpValidarResponse=function(){
		this.cCodIntPed=null;
		this.nCodPed=null;
		this.cCodStatus=null;
		this.cDescStatus=null;
	};
	this.omie_fail=function(){
		this.code=null;
		this.description=null;
		this.referer=null;
		this.fatal=null;
	};
};
module.exports = PedidoVendaFaturamentoJsonClient;