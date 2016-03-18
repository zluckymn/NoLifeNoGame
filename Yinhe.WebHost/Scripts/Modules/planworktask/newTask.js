function getRelations(taskId, paper, canv) {
    $.ajax({
        url: '/Projects/ProjArranged/GetTaskRelToFlowChart/' + taskId, cache: false,
        complete: function() { canv.unmask(); },
        success: function(data) {
            renderER(paper, data, canv);
        }
    });
}
function rpclick(taskId) {
    //2012-1-14地址修改projtaskdetail to ProjTaskInfo
    if (window.VerificationCode) {
        if (VerificationCode == "294728B6-FF56-4acd-A526-BFAFAC5E87D5") {
            window.open('/Projects/ProjArranged/projtaskdetail/' + taskId, '_blank');
        } else {
            window.open('/Projects/ProjArranged/ProjTaskInfo/' + taskId, '_blank');
        }
    } else {
    window.open('/Projects/ProjArranged/ProjTaskInfo/' + taskId, '_blank');
    }
}
function renderER(paper, data, canv) {
    var preTaskList = data.preTaskList, // 所有前置任务
        sucTaskList = data.sucTaskList, // 别人选自己的
        dt, i = 0, txt, len, x = 20, y = 20, w = 100, h, nw = 690, nh = 280, caltxt, nodes = {}, ct = 1;
    for (; dt = preTaskList[i]; ++i) { //taskId, status, statusName, relTaskType
        txt = dt.name; var taskId = dt.taskId;
        caltxt = calcTxt(txt);
        h = caltxt.h;
        if (ct%2 == 0){
            x += 20;
        } else {
            x = 20;
        }
        var rp = paper.rect(x, y, w, h), _tx = x + Math.floor(w/2), _ty = y + Math.floor(h/2),
            cont = caltxt.s.join('\n');
        rp.attr({"fill":"#bcccee", "cursor":'pointer'});
        rp.click(function(){ rpclick(taskId); });
        var label = paper.text(_tx, _ty, cont);
        label.attr({"font-family":"宋体", "font-size":11});
        y = y + h + 20;
        if (y > nh) {
            nh = y;
            paper.setSize(nw, nh);
        }
        dt.rp = rp;
        var _nid = 'wf'+ct; dt.id = ct;
        nodes[_nid] = {id:_nid, rp:rp, label:txt, pid:'0', dis:'0', isPoints:'0', points:'0'};
        setStat(paper, rp, dt.status); ct++;
    }
    function calcTxt(ts) {
        var len = String.getRealLen(ts);
        len = Math.ceil(len / 12);
        var cc = [], s = '', h = 0;
        for (var j = 1; j <= len; j++) {
            s = String.cut(ts, 12); h += 24;
            ts = ts.replace(s, '');
            cc.push(s);
        }
        return {s: cc, h: h};
    }
    // render self
    caltxt = calcTxt(data.taskName);
    h = caltxt.h;
    x = (nw - 150) / 2;
    y = (nh - h) / 2;
    var sf = paper.rect(x, y, 150, h);
    sf.attr({"gradient": "135-#b6c9e2-#d1ddee", "stroke-width": 2});
    var label0 = paper.text(x + 75, y + Math.floor(h/2), caltxt.s.join('\n'));
        label0.attr({"font-family":"宋体", "font-size":14});
    setStat(paper, sf, data.status);
    nodes['wf0'] = {id: 'wf0', rp:sf, label:'', pid:'0', dis:'0', isPoints:'0', points:'0'};
    x += 300; y = 20; var x1 = x;
    for (i = 0; dt = sucTaskList[i]; ++i) { //taskId, status, statusName, relTaskType
        txt = dt.name; var taskId = dt.taskId;
        caltxt = calcTxt(txt);
        if (i%2 == 1){
            x -= 20;
        } else {
            x = x1;
        }
        h = caltxt.h;
        var rp = paper.rect(x, y, w, h), _tx = x + Math.floor(w/2), _ty = y + Math.floor(h/2),
            cont = caltxt.s.join('\n');
        rp.attr({"fill":"#bcccee", "cursor":'pointer'});
        rp.click(function(){ rpclick(taskId); });
        var label = paper.text(_tx, _ty, cont);
        label.attr({"font-family":"宋体", "font-size":11});
        y = y + h + 20;
        if (y > nh) {
            nh = y;
            paper.setSize(nw, nh);
        }
        dt.rp = rp;
        var _nid = 'wf'+ct; dt.id = ct;
        nodes[_nid] = {id:_nid, rp:rp, label:txt, pid:'0', dis:'0', isPoints:'0', points:'0'};
        setStat(paper, rp, dt.status); ct++;
    }
    canv.height(nh);
    renderEdge(paper, sf, preTaskList, 'left', nodes);
    renderEdge(paper, sf, sucTaskList, 'right', nodes);
}
function setStat(paper, rp, stat) {
    var bb = rp.getBBox();
    if (statusImgs[stat]) {
        var imgStat = paper.image(imgUrl + statusImgs[stat], bb.x+bb.width-10, bb.y+bb.height-10, 10, 10);
        imgStat.attr("cursor", 'pointer');
        (function(nd, dtc0) {
            $(imgStat.node).tipsy({gravity: 'w', html:'true', title: function() {return '<div style="font-size:12px;">'+dtc0+'</div>';}});
        })(imgStat.node, statname[stat]);
    }
}
function renderEdge(paper, nowNode, list, dir, nodes) {
    var init = lrShape.init({p:paper, callback:function() {
    }, dblcallback:function() {
    }, showtitle: false
    }), dt, i = 0, xobj, from, to, p1 = 'e', p2 = 'w', st = '', rt;
    if (!init) return false;
    lrShape.setNodes(nodes);
    for (; dt = list[i]; ++i) { // ss fs ff sf
        rt = dt.relTaskType.toLowerCase(); p1 = 'e'; p2 = 'w'; st = '';
        if (rt == 'ss') {
            from = dt.id; to = '0'; st = 'twoway';
            p2 = 'n'; if (dir == 'right') {p1 = 'w'; p2 = 's';}
        } else if (rt == 'fs') {
            from = dt.id; to = '0';
            p2 = 'n';
            if (dir == 'right') { to = dt.id; from = '0'; p2 = 'w'; }
        } else if (rt == 'ff') {
            from = dt.id; to = '0'; st = 'twoway';
            p2 = 'n'; if (dir == 'right') {p1 = 'w'; p2 = 's';}
        } else { // sf
            from = '0'; to = dt.id;
            p1 = 'w'; p2 = 'e';
            if (dir == 'right') { from = dt.id; to = '0'; p2 = 's'; }
        }
        xobj = {'from':from+'|'+p1, 'to':to+'|'+p2, 'arrowOffset':0};
        lrShape.renderEdge(xobj, st);
        var rp = dt.rp, bb = rp.getBBox(), label,
            _tx, _ty;
        if (dir == 'left') {
            _tx = bb.x + bb.width + 15;
            _ty = bb.y + bb.height/2 - 5;
            label = paper.text(_tx, _ty, rt);
        } else {
            _tx = bb.x - 25;
            _ty = bb.y + bb.height/2 - 5;
            label = paper.text(_tx, _ty, rt);
        }
        label.attr({"font-family":"宋体", "font-size":12});
    }
}

var statusImgs = {2: 'smallIconOrange.png', 3: 'smallIconBlue.png', 4: 'smallIconGreen.png'},
    imgUrl = '/Content/Images/zh-cn/prodesiman/', statname = {2: '未开始', 3: '进行中', 4: '已完成'},
    selNodes = [];

function setNodeIds(taskId, isDel) {
    if (isDel) {
        selNodes.remove(taskId);
    } else {
        if (selNodes.indexOf(taskId) == -1) selNodes.push(taskId);
    }
    if (selNodes.length) {
        $("#gtaskmsg").removeAttr('disabled');
    } else {
        $("#gtaskmsg").attr('disabled', 'disabled');
    }
}
function doCompleteTask(taskName, uname) {
    var vw = YH.dom.getViewportWidth(), vh = YH.dom.getViewportHeight(), tpl,
        dfmsg = uname+'已经完成了“'+taskName+'”，请及时完成您的工作，谢谢！';
    tpl = '<div style="padding:5px 0;text-align:center;">完成任务的同时，你可以选择脉络图上的一个或多个节点，给相应的节点负责人进行发送任务完成通知。（可选）</div><iframe id="ifrmDiagram" width="'+(vw-200)+'" height="'+(vh-300)+'" src="/Projects/ProjArranged/ProjectDiagramSelect/'+projId+'?projId='+projId+'&tname='+taskName+'&taskId='+taskId+'"></iframe><div style="padding:5px 0;text-align:right;">消息正文：<input type="text" style="margin:0;padding:0;border:1px solid #ccc;font-size:12px;width:400px;" id="gtaskmsg" disabled value="'+dfmsg+'" /></div>';
    box(tpl, {boxid:'fwork',title:'完成任务进行消息提醒', contentType:'html',width:vw-200,modal:true,submit_BtnName:'完成任务',
        onOpen: function(o) {
        },
        submit_cb: function(o) {
            var ifrm = document.getElementById('ifrmDiagram'), msg;
            if (selNodes.length) {
                msg = $.trim($("#gtaskmsg").val());
                if (msg == '') msg = dfmsg;
                /*if (ifrm.attachEvent) {
                    ifrm.attachEvent("onload", function() {
                    });
                } else {
                    iframe.onload = function(){
                    };
                }*/
                var ids = selNodes.unique();
                $.ajax({
                    type: 'POST', url:'/Projects/ProjectManage/SendMsgToCheckedTask',
                    data: {taskId:taskId,sendTaskIds:ids.join(','),content:msg},
                    success: function(rs) {
                        if(rs.Success){
                        }else{
                            $.tmsg("m_exem", rs.msgError, {infotype:2});
                        }
                    }
                });
            }
            // 完成任务js
            $.ajax({
                type: 'POST', url: '/Persons/Assignment/ChangeTaskStatus/', data: { taskId: taskId, status: 4 },
                success: function(rs) {
                    if (rs.Success) {
                        $.tmsg("m_task", "操作成功！",{infotype:1});
                        //window.location.reload();
                        $("#taskInfoContainer").a3Load('/Projects/Assignment/CtrlTaskInfo/'+taskId+"?_t="+Math.random());
                    } else {
                        $.tmsg("m_task", rs.msgError,{infotype:2});
                    }
                }
            });
        }
    });
}

$(function() {

});