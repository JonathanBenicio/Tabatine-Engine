var OMIE_APP_KEY = 'PUT_YOUR_APP_KEY_HERE';
var OMIE_APP_SECRET = 'PUT_YOUR_APP_SECRET_HERE';

var XMLHttpRequest = require("xmlhttprequest").XMLHttpRequest;
var BancosCadastroJsonClient=function(){
	this._EndPoint="https://app.omie.com.br/api/v1/geral/bancos/";
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
	this.ConsultarBanco=function(
		fin_bancos_cadastro_chave,
		_cb
	){
		return this._Call(
			"ConsultarBanco",
			[
			fin_bancos_cadastro_chave
			],
			(_cb)?_cb:null
		);
	};
	this.ListarBancos=function(
		fin_bancos_list_request,
		_cb
	){
		return this._Call(
			"ListarBancos",
			[
			fin_bancos_list_request
			],
			(_cb)?_cb:null
		);
	};
	this.fin_banco_cadastro=function(){
		this.cnab_altve=null;
		this.cnab_altvl=null;
		this.cnab_cob=null;
		this.cnab_pag=null;
		this.crawler_sn=null;
		this.cwr_cobrem=null;
		this.cwr_cobret=null;
		this.cwr_pagrem=null;
		this.cwr_pagret=null;
		this.cwr_extr=null;
		this.obank_sn=null;
		this.obank_cobr=null;
		this.obank_extr=null;
		this.obank_pagt=null;
		this.obank_pix=null;
		this.cod_compen=null;
		this.cod_ispb=null;
		this.descond_sn=null;
		this.descond_qt=null;
		this.entf_cnpj=null;
		this.codigo=null;
		this.nome=null;
		this.tipo=null;
	};
	this.fin_bancos_cadastro_chave=function(){
		this.codigo=null;
	};
	this.fin_bancos_list_request=function(){
		this.pagina=null;
		this.registros_por_pagina=null;
		this.tipo=null;
		this.apenas_importado_api=null;
		this.ordenar_por=null;
		this.ordem_descrescente=null;
	};
	this.fin_bancos_list_response=function(){
		this.pagina=null;
		this.total_de_paginas=null;
		this.registros=null;
		this.total_de_registros=null;
		this.fin_banco_cadastro=null;
	};
	this.omie_fail=function(){
		this.code=null;
		this.description=null;
		this.referer=null;
		this.fatal=null;
	};
};
module.exports = BancosCadastroJsonClient;