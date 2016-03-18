/**
 * YHLoader.js
 * Copyright Yinhoo
 * Date:2011-1-4 by QBao
 */
var YinHoo = YinHoo || { version: '1.0', imgUrl: '/Content/Images/zh-cn/' };
(function(){
	/*
	 * support type:
	 * eg1. ~/YHUI/YHLoader.js?m=tree,calendar,xx.css
	 * eg2. ~/YHUI/YHLoader.js?m=m_common_yh,tree,r_jquery_jquery-1.4.2
	 * eg3. ~/YHUI/YHLoader.js?m=tree,/xx/oo/qq.js,calendar
	*/
	var modules = { // default modules
		calendar: {
		    js: "jquery.calendar.js",
			css: "calendar.css"
		},
		calendar1:{
			js: "jquery.calendar1.2.3.js",
			css: "calendar.css"
		},
		tree: {
			js: "jquery.tree.js",
			css: "tree.css"
		},
		menu: {
			js: "yh.menu.js",
			css: "yh.menu.css"
        },
        linkbutton: {
		    js: "yh.linkbutton.js",
		    css: "yh.linkbutton.css"
		},
		pagination: {
		    js: "yh.pagination.js",
		    css: "yh.pagination.css",
		    dependencies: ["linkbutton"]
		},
		resultselect: { // select results
			js: "yh.resultselect.js",
			css: "yh.resultselect.css",
			dependencies: ["tree"]
		}
	};
	var head = $N('head')[0], scripts = $N('script'), lib = null, existm = {}, userAgent = navigator.userAgent.toLowerCase();
	for(var i = 0; i < scripts.length; i++){
		var jsrc = scripts[i].src;
		if (jsrc.match(/YHLoader\.js(\W)/i)){
			if (lib = jsrc.match(new RegExp("(^|&|\\?)m=([^&]*)(&|$)","i"))){
				lib = lib[2].split(',');
			}
		}
	}
	var now = new Date().getTime().toString(36), isSafari = /webkit/.test(userAgent), isIE = /msie/.test(userAgent) && !/opera/.test(userAgent);
	var xhr = window.ActiveXObject ? new ActiveXObject("Microsoft.XMLHTTP") : new XMLHttpRequest();

	function globalEval(data) {
	    var script = $C("script");
	    script.type = "text/javascript";
	    if (isIE) {
	        script.text = data;
	    } else {
	        script.appendChild(document.createTextNode(data));
	    }
	    head.appendChild(script);
	}
	function loadJS(url, callback){
	    if (xhr) {
	        try {
	            xhr.open("GET", url, false);
	            xhr.send("");
	        } catch (ex) {
	            alert("xhr.open/send error, maybe cross domain");
	        }
	        if (xhr.readyState == 4) {
	            if (xhr.status == 0 || xhr.status == 200) {
	                globalEval(xhr.responseText);
	                if (callback) {
	                    callback.call(script);
	                }
	            } else if (xhr.status == 404) {
	                alert(url + "\nFile not found");
	            } else {
	                alert("unknown error");
	            }
	        }
	    } else {
	        alert("Your browser do not support XMLHttpRequest");
	    }
	}
	function loadCss(url, callback){
		var link = $C("link");
		link.rel = 'stylesheet';
		link.type = 'text/css';
		link.media = 'screen';
		link.href = url;
		head.appendChild(link);
		if (callback){
			callback.call(link);
		}
	}
	function mapPath(m){
		var suffix = /(\.js|\.css)/i.test(m) ? "" : ".js" + YHLoader.nocache ? "?_t=" + now : "";
		if(/^(m_|r_)/i.test(m)){ // load from modules or reference
			var sp = m.split("_"), tmp = "";
			for(var i = 1; i < sp.length; i++){
				tmp += "/" + sp[i];
			}
			return (/^m_/i.test(m) ? YHLoader.mPath : YHLoader.basePath) + tmp + suffix;
		}else if(m.indexOf("/") != -1){
			return m;
		}else{ // YHUI path
			return YHLoader.yhuiPath + m + suffix;
		}
	}

	function $id(el){
		return typeof el == 'string' ? document.getElementById(el) : el;
	}
	function $N(tagN, parentNode){
		parentNode = $id(parentNode) || document;
		return parentNode.getElementsByTagName(tagN);
	}
	function $C(tagN){
		return document.createElement(tagN);
	}

	function loadModule(m, callback){
		var mloading = [];
		if(typeof m == 'string'){
			add(m);
		}else{
			for(var i = 0; i < m.length; i++){
				add(m[i]);
			}
		}

		function add(ns){
			var mobj = null;
			if(mobj = modules[ns]){
				var dpd = modules[ns]['dependencies'];
				if (dpd){
					for(var j = 0; j < dpd.length; j++){
						add(dpd[j]);
					}
				}
				mloading.push(mobj['js']);
				if(mobj['css']) mloading.push(mobj['css']);
			}else{
				mloading.push(ns);
			}
		}
		function loadEach(){
			if(mloading.length){
				var p = mapPath(mloading.shift());
				if (!existm[p]) {
				    existm[p] = p;
					p.indexOf(".css") != -1 ? loadCss(p, loadEach()) : loadJS(p, loadEach());
				}else{
					loadEach();
				}
			}else{
				if(callback){
					callback();
				}
			}
		}

		loadEach();
	}

    var gdomain_ = typeof globalHostDomain_ != 'undefined' ? globalHostDomain_ : '';

	YHLoader = {
		basePath: gdomain_ + "/Scripts/Reference", // 
		mPath: gdomain_ + "/Scripts/Modules", // 
		yhuiPath: gdomain_ + "/Scripts/Reference/jQuery/YHUI/", // 
		nocache: false,
		load: function(m, callback){
			loadModule(m, callback);
		}
	}
	if(lib) loadModule(lib); // lib = ["m_common_yh", "tree", "r_jquery_jquery-1.4.2", "xx.css", "/xx/qq/yy.js"]
	window.using = YHLoader.load;
})();