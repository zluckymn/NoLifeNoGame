(function($) {
    $.fn.a3Load = function(url, params, fn) {
        var self = this;
        if (params) {
            if ($.isFunction(params)) {
                fn = params;
                params = null;
            }
        }
        $.ajax({
            url: url,
            data: params,
            success: function(rs) {
                $(self).html(rs);
                fn && fn.call(self);
            },
            dataType: 'html'
        });
    };
})(jQuery);

var tid = taskId, currentPage = 1, pageSize = 15, busiOrgId, pag, kw = '', tp = 0, _gupType = 'upfree';
function settype(type) {
    tp = type;
    var _tabct = { 0: '指引', 1: '标准', 2: '范例', 3: '文档' };
    $("#upnewpanel").find("b").html('添加' + _tabct[type]);
    getFileList('');
}
var tabs = $("#_gtabs").find("li"), _isFirstLoad = true;
tabs.each(function() {
    $(this).click(function() {
        var _tp = $(this).index();
        settype(_tp);
        tabs.removeClass('ks-active');
        $(this).addClass('ks-active');
    })
});
var _isCostConfig = false;
if ($("#taskType")[0] && $("#taskType").val() == '101') _isCostConfig = true;
//http://bts2.yinhooserv.com/browse/AIII-2343
if(verificationCode == "294728B6-FF56-4acd-A526-BFAFAC5E87D5")_isCostConfig = false;
if (!_isCostConfig) {
    $("#ltree").SelectTree({
        startXml: "/Projects/ProjArranged/GetBusinessOrgXml/?r=" + Math.random(),
        defaultShowItemLv: 5,
        _onClick: function(id, name, obj, node) {
            busiOrgId = id;
            currentPage = 1;
            getFileList('');
            modifyTabCount_(id);
        },
        _afterPrint: function(obj) {
            obj.selectTreeItem();
            $(tabs.get(tp)).trigger('click');
        }
    });
}
var _gnodecount = {};
function modifyBusOrgId_(orgId1, count) { // 修改左边树节点下的文档数
    if (orgId1 < 0) return;
    count = count || 0;
    var nd = document.getElementById('div' + orgId1);
    if (!nd) return;
    var txt = $(nd).find("div.name:first");
    if (txt[0]) {
        if (txt.find("span").length == 0) {
            txt.append('<span>('+count+')</span>');
        } else {
            txt.find('span').html('('+count+')');
        }
    }
}
function modifyTabCount_(orgId) { // 修改3个tab的count
    var dt = _gnodecount[orgId];
    if (!dt) return;
    $(tabs[0]).find("span").html('('+dt.guideCount+')');
    $(tabs[1]).find("span").html('('+dt.retTempCount+')');
    $(tabs[2]).find("span").html('('+dt.retSampCount+')');
}
//展示
function getFileList(keyword) {
    //if (!busiOrgId) return;
    //成果范例又说暂时需要跟着区域，暂时修改 by Radar
    var flist = $("#_gflist"), _busiOrgId = busiOrgId;//tp === 2 ? 0 : busiOrgId;;
    flist.mask('load...');
    $.ajax({
        type: 'get', url: '/Projects/ProjArranged/GetDesignGuide', cache: false,
        data: { taskId: taskId, busiOrgId: _busiOrgId, keyWord: keyword, pageSize: pageSize, current: currentPage, fileType: tp },
        dataType: 'json',
        success: function(rs) {
        
            var i = 0, rend = [], _o, _s, count = 1, idx, totalCount = 0, cls;
            for (; i < rs.length; ++i) {
                _o = rs[i];
                if (totalCount == 0) {
                    totalCount = _o.totalCount;
                }
                idx = count + (currentPage - 1) * pageSize; cls = '';
                if (idx % 2 === 0) cls = ' class="rowodd"';
                _s = '<tr' + cls + '><td idx="1">' + idx + '</td><td><a href="javascript:void(0)" onclick="ReadOnlineCustom(\'' + _o.fileId + '\',\'1\',0)" class="blue">' + _o.name + _o.ext + '</a></td><td align="center"> ' + _o.createUser + '</td><td align="center">' + _o.createDate + '</td><td align="center"> '+(tp===1?'<a href="javascript:void(0)" onclick="DownLoadByIdCustom(\'' + _o.fileId + '\',\'1\',0)" class="green">下载</a>':'') + (_o.isBlocFile != 1&&hasComplete=="False" ? ' <a href="javascript:void(0)" delflag="' + _o.relId + '" class="red">删除</a>' : '') + '</td></tr>';
                rend.push(_s); _s = null;
                count++;
            }
            var ct = flist.find('tbody');
            _s = rend.join('');
            ct.html(_s); _s = null; rend = null;
            if (totalCount > 0) buildPag(totalCount);
            flist.find("a[delflag]").each(function() {
                $(this).unbind('click').bind('click', function() {
                    delFile(this);
                });
            });
            $(tabs.get(tp)).find("span").html('('+totalCount+')');
            if (_isFirstLoad) {
                _isFirstLoad = false;
                $.ajax({
                    url: '/Projects/ProjArranged/GetBusinessOrgGuideCount', cache: false,
                    data: {taskId: taskId}, complete: function() { flist.unmask(); },
                    success: function(rs) {
                        var i = 0, dt;
                        for (; dt = rs[i]; ++i) {
                            var _orgId = dt.orgId || -5,
                                _allC = dt.allCount || 0;
                                _gnodecount[_orgId] = {"guideCount": dt.guideCount || 0, "retTempCount": dt.retTempCount || 0, "retSampCount": dt.retSampCount || 0, "allcount": dt.allCount || 0}
                                if (_orgId == _busiOrgId) {
                                    modifyTabCount_(_orgId);
                                }
                                modifyBusOrgId_(_orgId, _allC);
                        }
                    }
                });
            } else {
                flist.unmask();
            }
        }
    });
}
function delFile(o) {
    var tag = $(o), fileId = tag.attr('delflag');
    if (confirm('确定删除吗？\n删除后不可恢复！')) {
        $.post('/Projects/ProjArranged/DeleTaskFiles', { relIds: fileId }, function(data) {
            if (data.Success) {
                $(o).parent().parent().remove();
                reOrder();
                updateCt_(-1);
            }
            else {
                hiAlert(data.msgError, '删除失败！');
            }
        });
    }
}
function reOrder() {
    $("td[idx='1']").each(function(i) {
        $(this).text(i + 1);
    });
}
var sech = $("#opg_search").find("input");
sinpt = $(sech[0]); sbtn = $(sech[1]);
pag = $("#opg_pag");

$("#upnewpanel").click(function() {
    uploadnew(true);
});
sbtn.click(function() {
    searchFiles($.trim(sinpt.val()));
});
function searchFiles(keyword) {
    currentPage = 1;
    kw = keyword;
    getFileList(keyword);
}
function uploadnew(first) {
    var uppne = $("#uppne");
    if (first) $("#newlist").find("tr:gt(0)").remove();
    uppne.slideDown(function() {
        var pne = $(this);
        $("#cancelupload").unbind('click').bind('click', function() {
            pne.slideUp();
        });
        $("#_gselfile").unbind('click').bind('click', function() {
            SelBatch2('newlist', tid, busiOrgId, tp);
        });
        if (first) {
            SelBatch2('newlist', tid, busiOrgId, tp);
        }
    });
    $("#upnew").unbind('click').bind('click', function() {
        opGuidesSave(function() {
            uppne.slideUp();
            currentPage = 1;
            getFileList('');
        });
    });
}
function buildPag(totalCount) {
    pag.pagination({ total: totalCount, display_pc: 3, displayMsg: "", showInput: false, showPageList: false, showRefresh: false, loading: false, pageSize: pageSize,
        onSelectPage: function(current_page, pagesize) {
            currentPage = current_page;
            getFileList(kw);
        }
    });
}
var _fttableid, _ftbusinessOrgId, _ftfileType;
function SelBatch2(tableid, taskId, businessOrgId, fileType) {
    _ggisFolderUpload = false; _gupType = 'upguide';
    selFile.SelectFilesEx("上传文档", 0, 0, 0);
    _fttaskId = taskId; _fttableid = tableid;
    _ftbusinessOrgId = businessOrgId; _ftfileType = fileType;
}
//批量选择和上传文件
function SelBatch2_(_astr, tableid, taskId, businessOrgId, fileType) {
    //var arr = fs.GetSelectList(0, "");
    if (_astr == false) { return false; }
    var i = 0, arr = [], q;
	for(; i < _astr.length; ++i){
		q = _astr[i].split("|");
        var _pth = q[0];
		arr.push({"path":_pth});
	}
    var len = arr.length, l = $("tr[name='newupload']").length, html = "", i = 0, ct = [];
    if (len == 0) { return false; }
    var _busiOrgId = businessOrgId;
    //成果范例又说暂时需要跟着区域，暂时修改 by Radar
    //var _busiOrgId = fileType === 2 ? 0 : businessOrgId;
    for (; i < len; ++i) {
        var FileFullName = arr[i].path.substring(arr[i].path.lastIndexOf("\\") + 1),
            FileName = FileFullName.substring(0, FileFullName.lastIndexOf(".")),
            FileExt = fs.GetFileExt(arr[i].path);
        ct.push('<tr name="newupload1">');
        ct.push('<td style="display:none;"><input type="hidden" name="fileRels[' + l + '].fileRelationId" value="0" /><input type="hidden" name="fileRels[' + l + '].taskId" value="' + taskId + '" /><input type="hidden" name="fileRels[' + l + '].fileType" value="' + fileType + '" /><input type="hidden" name="fileRels[' + l + '].businessOrgId" value="' + _busiOrgId + '" /><input type="hidden" name="fileRels[' + l + '].remark" value="" /><input type="hidden" name="fileRels[' + l + '].status" value="" /></td>');
        ct.push('<td><input name="fileRels[' + l + '].FileName" value="' + FileName + '" style="width:100px" /></td>');
        ct.push('<td><input name="fileRels[' + l + '].Ext" value="' + FileExt + '" style="width:100px"/></td>');
        //ct.push('<td><input name="fileRels[' + l + '].Size" value="' + arr[i].size + '" style="width:100px"/></td>');
        ct.push('<td><input name="fileRels[' + l + '].LocalFilePath" value="' + arr[i].path + '" style="width:100px"/></td>');
        ct.push('<td><a href="javascript:;" onclick="_removeTmp(this);">移除</a></td>');
        ct.push('</tr>');
        l++;
    }
    html = ct.join('');
    $("#" + tableid).find('tr:last').after(html);
}
function updateCt_(num) { // 修改上传后的文件数显示
    var dt = _gnodecount[busiOrgId];
    num = num || 0;
    dt.allcount = dt.allcount + num;
    if (tp == 0) {
        dt.guideCount += num;
    } else if (tp == 1) {
        dt.retTempCount += num;
    } else if (tp == 2) {
        dt.retSampCount += num;
    }
    modifyBusOrgId_(busiOrgId, dt.allcount);
    modifyTabCount_(busiOrgId);
}
function opGuidesSave(callback) {
    $("tr[name='newupload1']").each(function(i) {
        var inpts = $(this).find("input");
        inpts.each(function() {
            this.name = this.name.replace(/\d/g, i);
        });
    });
    var formData = $("#_guploadForm").serialize();
    if (formData == "" && formData == null) return false;
    var box = $("#ltree").parent();
    box.mask('处理中，请稍候...');
    $.ajax({
        type: 'POST', url: '/Projects/ProjArranged/SaveTaskFiles', data: formData,
        complete: function() { box.unmask(); },
        success: function(data) {
            if (data.Success == false) {
                alert(data.msgError);
            } else {
                if (data.htInfo && data.htInfo.params) {
                    var files = eval("(" + data.htInfo.params + ")"),
                        len = files.length, str = "";
                    updateCt_(len);

                    for (var i = 0; i < len; i++) {
                        var file = files[i];
                        if (str == "") {
                            str = fs.bLocalSign + "|" + file.path + "|" + file.strParam;
                        } else {
                            str += "||" + fs.bLocalSign + "|" + file.path + "|" + file.strParam;
                        }

                    }
                    //批量添加到文件列表
                    if (str != "") {
                        fs.AddToListEx(str);
                    }
                    $.tmsg('m_upld', '操作成功！', { infotype: 1 });
                    callback && callback();
                }
            }
        }
    });
}
function _removeTmp(o) {
    $(o).parent().parent().remove();
}

// 交付物上传
function getFileName(path){
	if(path) path = path.replace("/", "\\");
	var pos1 = path.lastIndexOf('/');
	var pos2 = path.lastIndexOf('\\');
	var pos  = Math.max(pos1, pos2)
	if( pos<0 ){
		return path;
	}else{
		return path.substring(pos+1);
	}
}
var _ggisFolderUpload = false, _fttaskId, _fttodoId, _ftsecdPlanId, _ftClassStr, _ftobj;
function FreeUpload(taskId, todoId, secdPlanId, ClassStr, obj) {
    _ggisFolderUpload = false; _gupType = 'upfree';
    selFile.SelectFilesEx("上传文件", 0, 0, 0);
    _fttaskId = taskId; _fttodoId = todoId; _ftsecdPlanId = secdPlanId; _ftClassStr = ClassStr; _ftobj = obj;
}
function FreeUpload_(_astr, taskId, todoId, secdPlanId, ClassStr, obj) {
	var i = 0, arr = [], q, inpts = $("#g_"+ClassStr).find("input[name='tjfw']");
	for(; i < _astr.length; ++i){
		q = _astr[i].split("|");
        var _pth = q[0], _fname = getFileName(_pth);
		arr.push({"path":_pth});
	}
	if (arr.length > 0) {
        var path = "", name = "", taskRetFileId = 0, file = "", flag = false, x;
        for (x = 0; x < arr.length; x++) {
            path = arr[x].path;
			name = getFileName(path);
			arr[x].taskRetFileId = 0;
			inpts.each(function(){
				taskRetFileId = $(this).val();
				file = $.trim($(this).parent().next().find('a:first').text());
				if(name.toLowerCase() == file.toLowerCase()){
					arr[x].taskRetFileId = taskRetFileId;
					flag = true;
				}
			});
        }
		if(flag){
			var str = '<table cellpadding="0" cellspacing="0">';
			for (x = 0; x < arr.length; ++x) {
				str += '<tr><td>'+getFileName(arr[x].path)+'</td><td>';
				if(arr[x].taskRetFileId > 0){
					str += '<input type="checkbox" checked idex='+x+' taskRetFileId="'+arr[x].taskRetFileId+'" name="g-ck" />作为已存在的新版本';
				}
				str += '</td></tr>';
			}
			str += '</table>';
			(function(){
				var _arr = arr;
			box(str, {boxid:'showcf', title:'文件冲突处理', contentType:'html', submit_BtnName: "确定上传", cancel_BtnName: "取消", modal:true,
				onOpen:function(o){
					o.db.find("input[type='checkbox']").click(function(){
						var idx = parseInt($(this).attr("idex"), 10), taskRetFileId = $(this).attr("taskRetFileId");
						if($(this).attr("checked")){
							_arr[idx].taskRetFileId = taskRetFileId;
						}else{
							_arr[idx].taskRetFileId = 0;
						}
					});
				},
				submit_cb:function(o){
					UploadFilesFree(_arr, taskId, todoId, secdPlanId, ClassStr);
				}
			});
			})();
		}else{
			UploadFilesFree(arr, taskId, todoId, secdPlanId, ClassStr);
		}
	}
}

function FolderUpload(taskId, todoId, secdPlanId, ClassStr, obj) {
    _ggisFolderUpload = true;
	selFile.SelectFilesEx("文件夹拖拽上传", 0, 0, 0);
	_fttaskId = taskId; _fttodoId = todoId; _ftsecdPlanId = secdPlanId; _ftClassStr = ClassStr; _ftobj = obj;
}
function selFile::OnSaveFileList(strFile) {
	if(strFile == "") return;
	var str = strFile.split("?"), i, q, r = "";
	if (!_ggisFolderUpload) {
        if (_gupType == 'upfree') { // A3 AIII-1897
            FreeUpload_(str, _fttaskId, _fttodoId, _ftsecdPlanId, _ftClassStr, _ftobj);
        } else if (_gupType == 'upreport') {
            uploadReport_(str, _fttaskId, 3);
        } else if (_gupType == 'upguide') {
            SelBatch2_(str, _fttableid, _fttaskId, _ftbusinessOrgId, _ftfileType);
        }
        return false;
    }
	for(i = 0; i < str.length; ++i){
		q = str[i].split("|");
		r += "0|H|" + q[0] + "|H|" + q[1] + "|Y|";
	}
	r = r.replace(/\|Y\|$/g, "");
	$.ajax({type:'POST',
		url:'/Persons/WorkShop/CheckFileNameRepeat', data:{taskId:_fttaskId, todoId: _fttodoId, secdPlanId: _ftsecdPlanId, ClassStr: _ftClassStr, uploadFileList: r},
		success:function(dt){
			dt = dt.replace(/\\/g, "**");
			var rs = eval('('+dt+')');
			if(rs.success){
				rs.Result = rs.Result.replace(/\*\*/g, "\\");
				if(rs.IsRepeat != 0){
					var _tmp = '<table cellpadding="0" cellspacing="0">', arr = [];
					rs.Result = rs.Result.replace(/\|Y\|$/g, "");
					str = rs.Result.split("|Y|");
					for(i = 0; i < str.length; ++i){
						q = str[i].split("|H|"); arr[i] = {taskRetFileId:q[0], path:q[1], p1:q[2]};
						if(q[0] > 0){
							_tmp += '<tr><td>' + q[2] + "\\" +getFileName(q[1])+'</td><td>';
							_tmp += '<input type="checkbox" checked idex='+i+' taskRetFileId="'+q[0]+'" name="g-ck" />作为已存在的新版本';
							_tmp += '</td></tr>';
						}
					}
					_tmp += '</table>';
					(function(){
						var _arr = arr;
					box(_tmp, {boxid:'showcf', title:'文件冲突处理', contentType:'html', submit_BtnName: "确定上传", cancel_BtnName: "取消", modal:true,
						onOpen:function(o){
							o.db.find("input[type='checkbox']").click(function(){
								var idx = parseInt($(this).attr("idex"), 10), taskRetFileId = $(this).attr("taskRetFileId");
								if($(this).attr("checked")){
									_arr[idx].taskRetFileId = taskRetFileId;
								}else{
									_arr[idx].taskRetFileId = 0;
								}
							});
						},
						submit_cb:function(o){
							str = "";
							$.each(_arr, function(m, n){
								str += n.taskRetFileId + "|H|" + n.path + "|H|" + n.p1 + "|Y|";
							});
							str = str.replace(/\|Y\|$/g, "");
							SaveFolderUpload(str, _fttaskId, _fttodoId, _ftsecdPlanId, _ftClassStr);
						}
					});
					})();
				}else{
					SaveFolderUpload(rs.Result, _fttaskId, _fttodoId, _ftsecdPlanId, _ftClassStr);
				}
			}else{
				$.tmsg("m_upfile", rs.errors.msg,{infotype:2});
			}
		}
	});
}

function SaveFolderUpload(str, taskId, todoId, secdPlanId, ClassStr){
    if (str != "") {
        $.ajax({
            url: "/Persons/WorkShop/UploadFilesEx",
            type: 'post',
            data: {
                taskId: taskId,
                todoId: todoId,
                secdPlanId: secdPlanId,
                ClassStr: ClassStr,
                uploadFileList: str
            },
            success: function(data) {
                var str = data.split("|");
                var result = eval("(" + str[0] + ")");
                if (result.success) {
                    if (str.length > 1) {
                        if (str[1] != "") {
                            var files = eval("(" + str[1] + ")");
                            var len = files.length;
                            var str = "";
                            for (var i = 0; i < len; i++) {
                                var file = files[i];
                                if (str == "")
                                { str = fs.bLocalSign + "|" + file.path + "|" + file.strParam; }
                                else { str += "||" + fs.bLocalSign + "|" + file.path + "|" + file.strParam; }
                            }
                            fs.AddToListEx(str);
                        }
                    }
                    $.tmsg("m_upfile", "上传成功",{infotype:1});
                    $("#divDeliverFilesUpload").a3Load("/Projects/ProjArranged/DeliverFilesUpload/"+taskId);
                }
                else {
                    $.tmsg("m_upfile", "上传失败",{infotype:2});
                }
            },
            error: function(err) { alert("接口出错，请检查！"); },
            dataType: "html"
        });
    }
}

function UploadFilesFree(arr, taskId, todoId, secdPlanId, ClassStr) {
    if (arr.length > 0) {
        var path = "", addcount = 0;
        for (var x = 0; x < arr.length; x++) {
            if (path != "") { path += "|Y|"; }
            path += arr[x].taskRetFileId + "|H|" + arr[x].path;
			if(arr[x].taskRetFileId == 0) addcount++;
        }
		//alert(path);
		//return;
        $.ajax({
            url: "/Persons/WorkShop/UploadFilesEx",
            type: 'post',
            data: {
                taskId: taskId,
                todoId: todoId,
                secdPlanId: secdPlanId,
                ClassStr: ClassStr,
                uploadFileList: path
            },
            success: function(data) {
                var str = data.split("|");
                var result = eval("(" + str[0] + ")");
                if (result.success) {
                    if (str.length > 1) {
                        if (str[1] != "") {
                            var files = eval("(" + str[1] + ")");
                            var len = files.length;
                            var str = "";
                            for (var i = 0; i < len; i++) {
                                var file = files[i];
                                //fs.AddToList(file.path, file.strParam);
                                if (str == "")
                                { str = fs.bLocalSign + "|" + file.path + "|" + file.strParam; }
                                else { str += "||" + fs.bLocalSign + "|" + file.path + "|" + file.strParam; }
                            }
                            fs.AddToListEx(str);
                        }
                    }
                    $.tmsg("m_upfile", "上传成功",{infotype:1});
                    $("#divDeliverFilesUpload").a3Load("/Projects/ProjArranged/DeliverFilesUpload/"+taskId);
                    //如果任务未启动，默认启动任务，因此会刷新页面
                    if(Taskstatus==2){
                    window.location.reload();
                    }else{
                    if($("#taskInfoContainer").length>0)$("#taskInfoContainer").a3Load($("#taskInfoContainer").attr("url"));
                    }
                }
                else {
                    $.tmsg("m_upfile", result.msg,{infotype:2});
                }
            },
            error: function(err) { alert("接口出错，请检查！"); },
            dataType: "html"
        });
    }
}

function UploadFilesOne(taskRetFileId, todoId, ext) {

    var arr = fs.GetSelectList(1, "*" + ext);

    if (arr.length > 0) {
         var path = "";
        for (var x = 0; x < arr.length; x++) {
            if (path != "") { path += "|Y|"; }
            path += arr[x].path;
        }
        $.post('/Persons/WorkShop/UploadNewVersion',
                {
                    taskRetFileId: taskRetFileId,
                    uploadFileList: path,
                    todoId: todoId
                },
                function(data) {
                    if (data.Success) {
                        var files = eval("(" + data.msgError + ")");
                        for (var x = 0; x < files.length; x++) {
                            var path = files[x].path.replace(/\//g, "\\");
                            fs.AddToList(path, files[x].strParam);
                        }
						$.tmsg("m_upfile", "上传成功",{infotype:1});
						$("#divDeliverFilesUpload").a3Load("/Projects/ProjArranged/DeliverFilesUpload/"+taskId);
						//如果任务未启动，默认启动任务，因此会刷新页面
                    if(Taskstatus==2){
                    window.location.reload();
                    }else{
                    if($("#taskInfoContainer").length>0)$("#taskInfoContainer").a3Load($("#taskInfoContainer").attr("url"));
                    }
                    } else {
                        top.showInfoBar(data.msgError);
                    }
                }, 'json');

    }
}
function uploadNewVer(fileId) { // 汇报文档上传新版本
    var arr = fs.GetSelectList(1, "*.*");

    if (arr.length > 0) {
         var path = "";
        for (var x = 0; x < arr.length; x++) {
            if (path != "") { path += "|Y|"; }
            path += arr[x].path;
        }
        $.post('/Projects/ProjArranged/UploadNewVersion',
                {
                    fileId: fileId,
                    uploadFileList: path
                },
                function(data) {
                    if (data.Success) {
                        var files = eval("(" + data.msgError + ")");
                        for (var x = 0; x < files.length; x++) {
                            var path = files[x].path.replace(/\//g, "\\");
                            fs.AddToList(path, files[x].strParam);
                        }
						$.tmsg("m_upfile", "上传成功",{infotype:1});
						$("#divDeliverFilesUpload").a3Load("/Projects/ProjArranged/DeliverFilesUpload/"+taskId);
                    } else {
                        top.showInfoBar(data.msgError);
                    }
                }, 'json');

    }
}

function uploadReport(taskId, fileType) {
    _ggisFolderUpload = false; _gupType = 'upreport';
    selFile.SelectFilesEx("上传汇报文档", 0, 0, 0);
    _fttaskId = taskId;
}
function uploadReport_(_astr, taskId, fileType) {
    //var arr = fs.GetSelectList(0, "*.*");
    var i = 0, arr = [], q;
	for(; i < _astr.length; ++i){
		q = _astr[i].split("|");
        var _pth = q[0], _fname = getFileName(_pth);
		arr.push({"path":_pth, "name": _fname});
	}
    if (arr.length > 0) {
        var path = [], dt;
        for (var x = 0; x < arr.length; x++) {
            dt = arr[x];
            path.push('fileRels[' + x + '].fileRelationId=0');
            path.push('fileRels[' + x + '].taskId='+taskId);
            path.push('fileRels[' + x + '].fileType='+fileType);
            path.push('fileRels[' + x + '].businessOrgId=');
            path.push('fileRels[' + x + '].remark=');
            path.push('fileRels[' + x + '].status=');
            path.push('fileRels[' + x + '].FileName='+dt.name);
            path.push('fileRels[' + x + '].Ext='+fs.GetFileExt(dt.path));
            path.push('fileRels[' + x + '].Size='+dt.size);
            path.push('fileRels[' + x + '].LocalFilePath='+dt.path);
        }
        path = path.join('&');
        $.ajax({
            url: "/Projects/ProjArranged/SaveTaskFiles",
            type: 'post',
            data: path,
            success: function(data) {
                if (data.Success == false) {
                    $.tmsg("m_upld", data.msgError, {infotype:2});
                } else {
                    if (data.htInfo && data.htInfo.params) {
                        var files = eval("(" + data.htInfo.params + ")"),
                            len = files.length, str = "";

                        for (var i = 0; i < len; i++) {
                            var file = files[i];
                            if (str == "") {
                                str = fs.bLocalSign + "|" + file.path + "|" + file.strParam;
                            } else {
                                str += "||" + fs.bLocalSign + "|" + file.path + "|" + file.strParam;
                            }

                        }
                        //批量添加到文件列表
                        if (str != "") {
                            fs.AddToListEx(str);
                        }
                        $.tmsg('m_upld', '操作成功！', {infotype:1});
                        $("#divDeliverFilesUpload").a3Load("/Projects/ProjArranged/DeliverFilesUpload/"+taskId);
                        //如果任务未启动，默认启动任务，因此会刷新页面
                    if(Taskstatus==2){
                    window.location.reload();
                    }else{
                    if($("#taskInfoContainer").length>0)$("#taskInfoContainer").a3Load($("#taskInfoContainer").attr("url"));
                    }
                    }
                }
            },
            error: function(err) { alert("接口出错，请检查！"); }
        });
    }
}

function delJFW(id, delcfrm) {
    var title = '请注意！', tip = '确定删除吗？\n删除后不可恢复！'
    if (!id) {
        $.tmsg("m_jfw", "请选择要删除的交付物！");
        return false;
    } else {
        if (delcfrm) {
            title = '删除豁免前文件确认';
            tip = '该删除操作为彻底删除，\n删除后不可恢复，确定删除？';
        }
        hiConfirm(tip, title, function(r) {
	        if (r) {
                $.ajax({ type: 'POST',
                    url: '/Persons/WorkShop/PatchDelete/0', data: { fileIds: id },
                    success: function(data) {
                        if (data.Success) {
                            $.tmsg("m_jfw", "操作成功！", { infotype: 1 });
                             //如果任务未启动，默认启动任务，因此会刷新页面
                    if(Taskstatus==2){
                    window.location.reload();
                    }else{
                    if($("#taskInfoContainer").length>0)$("#taskInfoContainer").a3Load($("#taskInfoContainer").attr("url"));
                    }
                            $("#jfw"+id).remove();
                        } else {
                            $.tmsg("m_jfw", data.msgError, { infotype: 2 });
                        }
                    }
                });
            }
        });
    }
}

function delHB(id, o, delcfrm) {
    var tag = $(o), fileId = id, title = '请注意！', tip = '确定删除吗？\n删除后不可恢复！';
    if (delcfrm) {
        title = '删除豁免前文件确认';
        tip = '该删除操作为彻底删除，\n删除后不可恢复，确定删除？';
    }
    hiConfirm(tip, title, function(r) {
	    if (r) {
            $.post('/Projects/ProjArranged/DeleTaskFiles', { relIds: fileId }, function(data) {
                if (data.Success) {
                    //如果任务未启动，默认启动任务，因此会刷新页面
                    if(Taskstatus==2){
                    window.location.reload();
                    }else{
                    if($("#taskInfoContainer").length>0)$("#taskInfoContainer").a3Load($("#taskInfoContainer").attr("url"));
                    }
                    $(o).parent().parent().remove();
                }
                else {
                    hiAlert(data.msgError, '删除失败！');
                }
            });
	    }
    });
}

var a3plan_twork = (function() {
    var urls = {
        chgTskStat: '/Persons/Assignment/ChangeTaskStatus/'
    };
    return {
        changeTskStat: function(o, newStat, stageId,taskType) {
            if ($(o).hasClass("aorange")) {
                return;
            }
            var _t = newStat == 3 ? "启动" : (newStat == 4 ? "完成" : "未开始"),
			    _tip = "确定" + _t + "任务？";
            if (confirm(_tip)) {
                $.ajax({
                    type: 'POST', url: urls.chgTskStat, data: { taskId: taskId, status: newStat,stageId:stageId,taskType:taskType },
                    success: function(rs) {
                        if (rs.Success) {
                            $.tmsg("m_task", "操作成功！",{infotype:1});
                            //因为启动任务后又要显示材料和成本表，目前这两块没有做成控件，所以直接刷新页面了
                            window.location.reload();
                            //$("#taskInfoContainer").a3Load('/Projects/Assignment/CtrlTaskInfo/'+taskId+"?_t="+Math.random());
                            //2012-01-14 任务未启动时不能启动流程  BY Radar
                            //if(newStat==3){
                            //     $(".process-tip").show();
                            //}
                        } else {
                            $.tmsg("m_task", rs.msgError,{infotype:2});
                        }
                    }
                });
            }
        }
    }
})();