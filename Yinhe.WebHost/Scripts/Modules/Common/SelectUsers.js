/**
 * Copyright yinhoo
 * author: Qingbao.Zhao
 * 最后修改：2010-10-27
 */
/*
 * 组织架构、通用岗位、群组的人员选择
 */
 ;
 var SelectUsers = (function($) {
     var bb = null;
     var opt = {
         rType: 'select', // 值返回给的dom元素类型，有select、input、cb（值返回给callback）
         rselector: null, // 值返回给的dom元素的jQuery对象，如 $("#select")，针对文本框时，为存储userName，逗号隔开
         rselector1: null, // 针对返回值给input时，存储userid，逗号隔开
         multiSel: true, // 是否允许多选
         prt: null, // 父子弹窗
         single: false // 新单选
     };
     var tpl = "", _cls, _tag, _width;
     return {
         init: function(op) {
             if (typeof (op) == undefined) {
                 alert("配置出错！请检查...");
                 return false;
             }
             $("#addSysUserModule").empty().attr("id", "addSysUserModule1");
             _cls = "shadow-container";
             _width = op.single ? 650 : 830;
             opt = $.extend(opt, op, { boxid: "uSel", title: "人员选择", contentType:'html', width:_width,cls: _cls, zIndex: 10050, setZIndex: false });
             tpl = ['<div class="contain"><table width="100%">',
                '<tr>',
                '	<td valign="top">',
                '		<div class="add-people" style="margin:5px 8px">',
                '			<dt style="height: 30px;display:none">',
                '				<input name="txttree" id="txttree" type="text" size="25" onkeypress="txtTreeKeyPressFunc()" />',
                '				<input type="button" name="btnTreeSearch" id="btnTreeSearch" onclick="btnTreeSearchFunc()" value=" 搜索 " /> <input type="button" id="btnCannelSearch" value=" 重置 " onclick=\'', '$("#txttree").attr("value","");SelTree();\'/>',
                '			</dt>',
                '			<dt class="teee">',
                '			<div style=" background-color:#f7f7f7; line-height:25px">',
                '				<span><input type="radio" name="radType" value="orgpost" onclick="SelTree();" checked />组织架构</span>',
                '				<span style="display:none;"><input type="radio" name="radType" value="compost" onclick="SelTree();" />通用岗位</span>',
                '				<span style="display:none;"><input type="radio" name="radType" value="group" onclick="SelTree();" />群组</span>',
                '			</div>',
                '			<div style="width:250px; height:400px;" id="ifrm"></div>',
                '			</dt>',
                '		</div>',
                '	</td>',
                '	<td valign="top">',
                '       <div style="margin-top:10px; margin-bottom:10px"><input class="inputborder" name="input" id="txtSearchUser"  onkeypress="txtSearchUserFunc()" type="text" size="25" />',
                '            <a class="btn_05"  href="javascript:void(0);" id="btnSearchUser" onclick="btnSearchUserFunc()" >搜索<span></span></a></div>',
                '		<table style=" margin-top:5px">',
                '			<tr>',
                '				<td><select hidefocus="true" id="selUser" multiple="multiple" name="select5"' + (opt.single ? "" : " ondblclick=\"adds();\"") + ' style="height: 350px; width: 250px;"></select></td>',
                '				<td' + (opt.single ? " style=\"display:none;\"" : "") + '>',
                '					<div align="center">',
                '						<input type="image" id="sladd" hidefocus="true" onclick="adds();" src="/Content/images/common/btn-move.gif" style="margin:6px" /><br />',
                '						<input type="image" id="delete" hidefocus="true" onclick="dels();" src="/Content/images/common/btn-move2.gif" />',
                '					</div>',
                '				</td>',
                '				<td' + (opt.single ? " style=\"display:none;\"" : "") + '><select id="selAdd" hidefocus="true" multiple="multiple" name="D2" ondblclick="dels();" style="height: 350px; width: 250px;"></select></td>',
                '			</tr>',
                '			<tr' + (opt.single ? " style=\"display:none;\"" : "") + '>',
                '               <td align="center" height="30"><a class="btn_01" href="javascript:void(0);" onclick="adds();" >添加<span></span></a></td>',
                //'				<td align="center"><input name="Submit3" type="button" hidefocus="true" class="btn" value="添 加" onclick="adds();" /></td>',
                '				<td>&nbsp;</td>',
                '               <td align="center"><a class="btn_01" href="javascript:void(0);" onclick="dels();" >删除<span></span></a></td>',
                //'				<td align="center"><input name="Submit3" type="button" hidefocus="true" class="btn" value="删 除" onclick="dels();" /></td>',
                '			</tr>',
                '			<tr' + (opt.single ? " style=\"display:none;\"" : "") + '>',
                '				<td height="50" align="center" valign="middle" class="gray">多选操作说明</td>',
                '				<td>&nbsp;</td>',
                '			</tr>',
                '		</table>',
                '		<input id="AddedUsersName" type="hidden" value="" /><input id="AddedUsers" type="hidden" value="," /><input id="DeledUsers" type="hidden" value="," />',
                '	</td>',
                '</tr>',
            '</table></div>'].join("");
             bb = box(tpl, opt); _tag = "selAdd";
             if (!opt.multiSel) { // 是否只允许单选
                 _tag = opt.single ? "selUser" : "selAdd", seladd = bb.fbox.find("select[id='" + _tag + "']");
                 seladd.click(function() {
                     var selIndex = this.selectedIndex;
                     $('#' + _tag + ' option').removeAttr("selected");
                     this.options[selIndex].selected = true;
                 });
             }
             this.commit();
         },
         commit: function() {
             var sf = this;
             bb.options.submit_cb = function() {
                 var rs = {};
                 if (opt.multiSel) {
                     $('#' + _tag + ' option', bb.fbox).each(function() {
                         rs[$(this).val()] = $(this).text();
                     });
                 } else {
                     if ($('#' + _tag + ' option:selected').length == 0) {
                         alert("请选择一个用户！");
                         return false;
                     }
                     $('#' + _tag + ' option:selected', bb.fbox).each(function() {
                         rs[$(this).val()] = $(this).text();
                     });
                 }
                 if (opt.rType == 'select') {
                     if ($(opt.rselector).length > 0) {
                         var hasi = false;
                         for (var uid in rs) {
                             hasi = false;
                             if ($(opt.rselector).find("option").length > 0) {
                                 $(opt.rselector).find("option").each(function() {
                                     if (uid == $(this).val()) hasi = true;
                                 });
                                 if (!hasi) $(opt.rselector).append("<option value='" + uid + "'>" + rs[uid] + "</option>")
                             } else {
                                 $(opt.rselector).append("<option value='" + uid + "'>" + rs[uid] + "</option>");
                             }
                         }
                     }
                 } else if (opt.rType == 'input') {
                     var userId = "", userName = "";
                     for (var uid in rs) {
                         userId = userId + "," + uid;
                         userName = userName + "," + rs[uid];
                     }
                     userId = userId.substr(1);
                     userName = userName.substr(1);
                     if ($(opt.rselector).length > 0) { // userName
                         $(opt.rselector).val(userName);
                     }
                     if ($(opt.rselector1).length > 0) { // userId
                         $(opt.rselector1).val(userId);
                     }
                 } else {
                     opt.callback(rs);
                 }
                 //return false
             }
         }
     }
 })(jQuery);

 var SelectUsersZHHY = (function ($) {
     var bb = null;
     var opt = {
         rType: 'select', // 值返回给的dom元素类型，有select、input、cb（值返回给callback）
         rselector: null, // 值返回给的dom元素的jQuery对象，如 $("#select")，针对文本框时，为存储userName，逗号隔开
         rselector1: null, // 针对返回值给input时，存储userid，逗号隔开
         multiSel: true, // 是否允许多选
         prt: null, // 父子弹窗
         single: false // 新单选
     };
     var tpl = "", _cls, _tag, _width;
     return {
         init: function (op) {
             if (typeof (op) == undefined) {
                 alert("配置出错！请检查...");
                 return false;
             }
             $("#addSysUserModule").empty().attr("id", "addSysUserModule1");
             _cls = "shadow-container";
             _width = op.single ? 650 : 710;
             opt = $.extend(opt, op, { boxid: "uSel", title: "人员选择", contentType: 'html', width: _width, cls: _cls, zIndex: 10050, setZIndex: false });
             tpl = ['<div class="contain"><table width="100%">',
                '<tr>',
                '	<td valign="top">',
                '		<div class="add-people boxmargin">',
                '			<dt style="height: 30px;display:none">',
                '				<input name="txttree" id="txttree" type="text" size="25" onkeypress="txtTreeKeyPressFunc()" />',
                '				<input type="button" name="btnTreeSearch" id="btnTreeSearch" onclick="btnTreeSearchFunc()" value=" 搜索 " /> <input type="button" id="btnCannelSearch" value=" 重置 " onclick=\'', '$("#txttree").attr("value","");SelTree();\'/>',
                '			</dt>',
                '			<dt class="teee">',
                '			<div style=" background-color:#f7f7f7; line-height:25px">',
                '				<span><input type="radio" name="radType" value="orgpost" onclick="SelTree();" checked />组织架构</span>',
                '				<span style="display:none;"><input type="radio" name="radType" value="compost" onclick="SelTree();" />通用岗位</span>',
                '				<span style="display:none;"><input type="radio" name="radType" value="group" onclick="SelTree();" />群组</span>',
                '			</div>',
                '			<div style="width:250px; height:400px;" id="ifrm"></div>',
                '			</dt>',
                '		</div>',
                '	</td>',
                '	<td valign="top">',
                '       <div style="margin-top:10px; margin-bottom:10px"><input class="inputborder" name="input" id="txtSearchUser"  onkeypress="txtSearchUserFunc()" type="text" size="30" />',
                '            <a class="btn_05" style="color:#0099dd" href="javascript:void(0);" id="btnSearchUser" onclick="btnSearchUserFunc()" >搜索<span></span></a></div>',
                '		<table style=" margin-top:5px">',
                '			<tr>',
                '				<td><select hidefocus="true" id="selUser" multiple="multiple" name="select5"' + (opt.single ? "" : " ondblclick=\"adds();\"") + ' style="height: 350px; width: 180px;"></select></td>',
                '				<td' + (opt.single ? " style=\"display:none;\"" : "") + '>',
                '					<div align="center">',
                '						<input type="image" id="sladd" hidefocus="true" onclick="adds();" src="/Content/images/common/btn-move.gif" style="margin:6px" /><br />',
                '						<input type="image" id="delete" hidefocus="true" onclick="dels();" src="/Content/images/common/btn-move2.gif" />',
                '					</div>',
                '				</td>',
                '				<td' + (opt.single ? " style=\"display:none;\"" : "") + '><select id="selAdd" hidefocus="true" multiple="multiple" name="D2" ondblclick="dels();" style="height: 350px; width: 180px;"></select></td>',
                '			</tr>',
                '			<tr' + (opt.single ? " style=\"display:none;\"" : "") + '>',
                '               <td align="center" height="30"><a class="btn_01" href="javascript:void(0);" onclick="adds();" >添加<span></span></a></td>',
             //'				<td align="center"><input name="Submit3" type="button" hidefocus="true" class="btn" value="添 加" onclick="adds();" /></td>',
                '				<td>&nbsp;</td>',
                '               <td align="center"><a class="btn_01" href="javascript:void(0);" onclick="dels();" >删除<span></span></a></td>',
             //'				<td align="center"><input name="Submit3" type="button" hidefocus="true" class="btn" value="删 除" onclick="dels();" /></td>',
                '			</tr>',
                '			<tr' + (opt.single ? " style=\"display:none;\"" : "") + '>',
                '				<td height="50" align="center" valign="middle" class="gray">多选操作说明</td>',
                '				<td>&nbsp;</td>',
                '			</tr>',
                '		</table>',
                '		<input id="AddedUsersName" type="hidden" value="" /><input id="AddedUsers" type="hidden" value="," /><input id="DeledUsers" type="hidden" value="," />',
                '	</td>',
                '</tr>',
            '</table></div>'].join("");
             bb = box(tpl, opt); _tag = "selAdd";
             if (!opt.multiSel) { // 是否只允许单选
                 _tag = opt.single ? "selUser" : "selAdd", seladd = bb.fbox.find("select[id='" + _tag + "']");
                 seladd.click(function () {
                     var selIndex = this.selectedIndex;
                     $('#' + _tag + ' option').removeAttr("selected");
                     this.options[selIndex].selected = true;
                 });
             }
             this.commit();
         },
         commit: function () {
             var sf = this;
             bb.options.submit_cb = function () {
                 var rs = [], uid, uname;
                 if (opt.multiSel) {
                     $('#' + _tag + ' option', bb.fbox).each(function () {
                         rs.push({ userId: $(this).val(), name: $(this).text() });
                         //rs[$(this).val()] = $(this).text();
                     });
                 } else {
                     if ($('#' + _tag + ' option:selected').length == 0) {
                         alert("请选择一个用户！");
                         return false;
                     }
                     $('#' + _tag + ' option:selected', bb.fbox).each(function () {
                         rs.push({ userId: $(this).val(), name: $(this).text() });
                         //rs[$(this).val()] = $(this).text();
                     });
                 }
                 if (opt.rType == 'select') {
                     if ($(opt.rselector).length > 0) {
                         var hasi = false;
                         for (var index in rs) {
                             hasi = false;
                             uid = rs[index].userId; //用户ID
                             uname = rs[index].name; //用户名
                             if ($(opt.rselector).find("option").length > 0) {
                                 $(opt.rselector).find("option").each(function () {
                                     if (uid == $(this).val()) hasi = true;
                                 });
                                 if (!hasi) $(opt.rselector).append("<option value='" + uid + "'>" + uname + "</option>")
                             } else {
                                 $(opt.rselector).append("<option value='" + uid + "'>" + uname + "</option>");
                             }
                         }
                     }
                 } else if (opt.rType == 'input') {
                     var userId = "", userName = "";
                     for (var index in rs) {
                         userId = userId + "," + rs[index].userId;
                         userName = userName + "," + rs[index].name;
                     }
                     userId = userId.substr(1);
                     userName = userName.substr(1);
                     if ($(opt.rselector).length > 0) { // userName
                         $(opt.rselector).val(userName);
                     }
                     if ($(opt.rselector1).length > 0) { // userId
                         $(opt.rselector1).val(userId);
                     }
                 } else {
                     opt.callback(rs);
                 }
                 //return false
             }
         }
     }
 })(jQuery);