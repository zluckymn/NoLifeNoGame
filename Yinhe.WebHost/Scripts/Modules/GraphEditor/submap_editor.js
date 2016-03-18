/**
 * map editor
 * 10/8/2012
 * Qingbao.Zhao
 */
MT.define(function(require, exports, module) {
    var M = MT, tpls,
        uuid = M.math.uuid(8);

    require('/Scripts/Modules/GraphEditor/jquery.colorpicker');

    var editorMap = {
        1: 'edt_font',
        2: 'edt_fontSize',
        3: 'edt_fontColor',
        4: 'edt_fontBold',
        5: 'edt_fontItalic',
        6: 'edt_fontUnderline',
        7: 'edt_txtLeft',
        8: 'edt_txtMid',
        9: 'edt_txtRight',
        10: 'edt_lineColor',
        11: 'edt_fillColor',
        12: 'edt_lineType',
        13: 'edt_lineSize',
        14: 'edt_doline',
        15: 'edt_lineArr',
        16: 'edt_toFront',
        17: 'edt_toBack',
        18: 'paperSetting',
        19: 'edt_link',
        20: 'edt_createNew',
        21: 'edt_save',
        22: 'edt_openex'
    },
        shapeProp = {
        "fill": true,
        "stroke": true,
        "stroke-dasharray": true,
        "stroke-width": true
    };

    function SubMap(cfg) {
        cfg = cfg || {};
        var me = this;
        MT.mixin(me, cfg, true);
        me.tpls = MT.mixin(me.tpls || {}, tpls);
        me.title = me.title || '图形编辑器';
        me.index = 0;
        me.initCt();
    }

    SubMap.prototype = {
        nowid: null,
        defV: {
            size: 12,
            family: '宋体',
            lineFill: '#000',
            lineType: 1,
            lineArr: ''
        },
        nodes: {}, // node set
        relTree: {}, // relative between nodes
        lines: {},
        nowTranf: {},
        dcache: {},
        ang: {'n': 270, 's': 90, 'e': 0, 'w': 180},
        initCt: function() {
            this.createLinePne();
            this.createLineArrPne();
            var me = this,
                panel = new MT.ui.Panel({
                renderTo: document.body,
                title: me.title, bodyStyle:'background-color:#ababab',
                autoScroll: true, fitCt: true,
                html: '<div id="_nsubwayct" style="margin:0;padding:0;left:0;top:0;"></div>',
                items: [
                    {
                        mType: 'toolbar',
                        items: [
                            {mType: 'textitem', text: '左侧可选图形', width: 120},'-',
                            {mType: 'button', id: editorMap[1], iconCls: 'efont', split: true, label: '宋体', tooltip: '字体', disabled: true, menu: {id: 'edt_menu_font',
                                renderTo: document.body, showSeparator: false,
                                items: [
                                    {mType: 'menuItem', text: '宋体'},
                                    {mType: 'menuItem', text: '新宋体'},
                                    {mType: 'menuItem', text: '仿宋'},
                                    {mType: 'menuItem', text: '楷体'},
                                    {mType: 'menuItem', text: '黑体'},
                                    {mType: 'menuItem', text: '微软雅黑'},
                                    {mType: 'menuItem', text: 'Arial'},
                                    {mType: 'menuItem', text: 'Arial Black'},
                                    {mType: 'menuItem', text: 'Times New Roman'},
                                    {mType: 'menuItem', text: 'Courier New'},
                                    {mType: 'menuItem', text: 'Tahoma'},
                                    {mType: 'menuItem', text: 'Verdana'}, '-',
                                    {mType: 'menuItem', diy: true, text: '自定义'}
                                ]
                            }},
                            {mType: 'button', id: editorMap[2], iconCls: 'esize', split: true, label: '12', tooltip: '大小', disabled: true, menu: {id: 'edt_menu_fontSize',
                                renderTo: document.body, showSeparator: false,
                                items: [
                                    {mType: 'menuItem', text: '6px'},
                                    {mType: 'menuItem', text: '8px'},
                                    {mType: 'menuItem', text: '9px'},
                                    {mType: 'menuItem', text: '10px'},
                                    {mType: 'menuItem', text: '11px'},
                                    {mType: 'menuItem', text: '12px'},
                                    {mType: 'menuItem', text: '14px'},
                                    {mType: 'menuItem', text: '16px'},
                                    {mType: 'menuItem', text: '18px'},
                                    {mType: 'menuItem', text: '24px'},
                                    {mType: 'menuItem', text: '32px'},
                                    {mType: 'menuItem', text: '48px'},
                                    {mType: 'menuItem', text: '72px'}, '-',
                                    {mType: 'menuItem', diy: true, text: '自定义'}
                                ]
                            }},
                            {mType: 'button', id: editorMap[3], disabled: true, iconCls: 'efcolor', split: true, tooltip: '字体颜色'},'-',
                            {mType: 'button', id: editorMap[4], disabled: true, iconCls: 'ebold', tooltip: '粗体'},
                            {mType: 'button', id: editorMap[5], disabled: true, iconCls: 'eital', tooltip: '斜体'},
                            {mType: 'button', id: editorMap[6], disabled: true, iconCls: 'eunderline', tooltip: '下划线'},'-',
                            {mType: 'button', id: editorMap[7], disabled: true, iconCls: 'ealeft', tooltip: '居左'},
                            {mType: 'button', id: editorMap[8], disabled: true, iconCls: 'eamid', tooltip: '居中'},
                            {mType: 'button', id: editorMap[9], disabled: true, iconCls: 'earight', tooltip: '居右'},'-',
                            {mType: 'button', id: editorMap[10], iconCls: 'elcolor', split: true, tooltip: '线条颜色'},
                            {mType: 'button', id: editorMap[11], iconCls: 'ebcolor', split: true, tooltip: '填充颜色'},'-',
                            {mType: 'button', id: editorMap[12], iconCls: 'eltype', split: true, tooltip: '线条类型'},
                            {mType: 'button', id: editorMap[13], iconCls: 'elsize', split: true, tooltip: '线条粗细', menu: {id: 'edt_menu_lineSize',
                                renderTo: document.body, showSeparator: false,
                                items: [
                                    {mType: 'menuItem', text: '1'},
                                    {mType: 'menuItem', text: '2'},
                                    {mType: 'menuItem', text: '3'},
                                    {mType: 'menuItem', text: '4'},
                                    {mType: 'menuItem', text: '5'},
                                    {mType: 'menuItem', text: '6'},
                                    {mType: 'menuItem', text: '7'},
                                    {mType: 'menuItem', text: '8'},
                                    {mType: 'menuItem', text: '9'}, '-',
                                    {mType: 'menuItem', diy: true, text: '自定义'}
                                ]
                            }},'-',
                            {mType: 'splitButton', id: editorMap[14], iconCls: 'edoline', tooltip: '连线', split: true, toggleGroup: 'shape', lineType: 1,
                             menu: {id: 'edt_menu_lineType',
                                renderTo: document.body,
                                items: [
                                    {
                                        mType: 'menuItem', lineType: 1, iconCls: 'edoline',
                                        text: '折线'
                                    }, '-',
                                    {
                                        mType: 'menuItem', lineType: 2, iconCls: 'edoSline',
                                        text: '直线'
                                    }
                                ]
                            }},
                            {mType: 'button', id: editorMap[15], iconCls: 'elinearr', split: true, tooltip: '线端'},
                            {mType: 'button', id: editorMap[16], iconCls: 'etofront', tooltip: '置顶'},
                            {mType: 'button', id: editorMap[17], iconCls: 'etoback', tooltip: '置底'}, '-',
                            {mType: 'button', id: editorMap[19], iconCls: 'elink', tooltip: '超链接', handler: function() {me.editLink();}},'-',
                            {mType: 'button', id: editorMap[18], iconCls: 'setting', label: '设置', handler: function() {me.paperSetting();}},
                            {mType: 'button', id: editorMap[20], iconCls: 'newfile', label: '新建', handler: function() {me.resetPaper();}},
                            {mType: 'button', id: editorMap[22], iconCls: 'repimg',pack: 'right',label: '打开', handler: function() { me.openEx(); } },
                            {mType: 'button', id: editorMap[21], iconCls: 'save',pack: 'right',label: '保存',
                                handler: function() {
                                    me.editFileName();
                                }
                            }
                        ]
                    },
                    {
                        mType: 'toolbar',
                        vertical: true,
                        dock: 'left', width: 35,
                        items: [
                            {
                                mType: 'button', id: 'btn_moveNode',
                                scale: 'medium', toggleGroup: 'shape',
                                iconCls: 'move', tooltip: '移动(V)',
                                handler: function() {
                                    me.toDraw(this.iconCls, this);
                                }
                            }, '-',
                            {
                                mType: 'button',
                                scale: 'medium', toggleGroup: 'shape',
                                iconCls: 'text', tooltip: '文本',
                                handler: function() {
                                    me.toDraw(this.iconCls, this);
                                }
                            }, '-',
                            {
                                mType: 'button',
                                scale: 'medium', toggleGroup: 'shape',
                                iconCls: 'circle', tooltip: '圆',
                                handler: function() {
                                    me.toDraw(this.iconCls, this);
                                }
                            }, '-',
                            {
                                mType: 'button',
                                scale: 'medium', toggleGroup: 'shape',
                                iconCls: 'rect', tooltip: '矩形',
                                handler: function() {
                                    me.toDraw(this.iconCls, this);
                                }
                            }, '-',
                            {
                                mType: 'button',
                                scale: 'medium', toggleGroup: 'shape',
                                iconCls: 'ellipse', tooltip: '椭圆',
                                handler: function() {
                                    me.toDraw(this.iconCls, this);
                                }
                            }, '-',
                            {
                                mType: 'button',
                                scale: 'medium', toggleGroup: 'shape',
                                iconCls: 'rectc', tooltip: '圆角矩形',
                                handler: function() {
                                    me.toDraw(this.iconCls, this);
                                }
                            }, '-',
                            {
                                mType: 'button',
                                scale: 'medium', toggleGroup: 'shape',
                                iconCls: 'rhombus', tooltip: '棱形',
                                handler: function() {
                                    me.toDraw(this.iconCls, this);
                                }
                            }, '-',
                            {
                                mType: 'button',
                                scale: 'medium', toggleGroup: 'shape',
                                iconCls: 'triangle', tooltip: '三角形',
                                handler: function() {
                                    me.toDraw(this.iconCls, this);
                                }
                            }, '-',
                            {
                                mType: 'button',
                                scale: 'medium', toggleGroup: 'shape',
                                iconCls: 'rects', tooltip: '矩形一',
                                handler: function() {
                                    me.toDraw(this.iconCls, this);
                                }
                            }, '-',
                            {
                                mType: 'button',
                                scale: 'medium', toggleGroup: 'shape',
                                iconCls: 'hexagon', tooltip: '六边形',
                                handler: function() {
                                    me.toDraw(this.iconCls, this);
                                }
                            }
                        ]
                    }
                ],
                tools: [
                    {
                        type:'qinfo',
                        tooltip: '信息',
                        pack: 'right'
                    }
                ]
            });
            panel.mask("初始化中，请稍后...");
            MT.on(window, "resize", function() {
                panel.fitContainer();
                panel.layout.reLayout();
            });
            me.bindEditPne(panel);
            me.ct = panel;
            me.changeTitle();
            me.initPaper(); // init paper
            if (me.loadId) {
                me.doLoadData_(me.loadId, me.mapName);
            }
        },
        paperCfg: function(setting) {
            setting = setting || {};
            var me = this, paperct = me.paperct, w, h, size = paperct.getSize();
            MT.mixin(me.dcache, setting, true);
            if (setting.bgImg && setting.bgImg != "") {
                me.setBgImg();
            } else {
                me.dcache.bgImg = '';
                paperct.setStyle({'background-image': 'none'});
            }
            w = setting.width || me.dcache.width;
            h = setting.height || me.dcache.height;
            paperct.setStyle({left:0, top:0, "background-color": me.dcache.bgColor, width: w+'px', height: h+'px'}); // reset paper container.
            me.paper.setSize(w, h);
            /*if (w < size.width) {
                paperct.center(me.ct.childCt.body);
            }*/
        },
        reCalcPaper: function() {
        },
        paperSetting: function() { // paper setting panel
            var me = this;

            if (!me.ptSetting) {
                me.ptSetting = new MT.ui.Panel({
                    renderTo: document.body,
                    noHeader: true,
                    width: 220,
                    height: 200, floating: true,
                    closeAction: 'hide', bodyCls: 'pnebody',
                    visible: true,
                    bodyPadding: 5,
                    listeners: {
                        'show': function(w) {
                            me.delKeyEnabled = false;
                            me.getBgImgData(w);
                        }
                    },
                    items: [
                        {
                            mType: 'toolbar',
                            dock: 'bottom',
                            items: [
                            {mType: 'button', pack: 'right', label: '确 定',
                                handler: function(o, e) {
                                    me.getSetting(o, e);
                                    $("#colorpanel").hide();
                                    me.ptSetting.hide();
                                    me.delKeyEnabled = true;
                                }
                            },
                            {mType: 'button', pack: 'right', label: '取 消',
                                handler: function() {
                                    $("#colorpanel").hide();
                                    me.ptSetting.hide();
                                    me.delKeyEnabled = true;
                                }
                            }
                            ]
                        }
                    ]
                });
            }
            me.unTrans();
            me.nowNode = null;
            me.ptSetting.setPagePosition(me.ptSetting.node.getAlignToXY(MT.CompMgr.get(editorMap[18]).node, 'tr-br?'));
            me.ptSetting.show();
        },
        getBgImgData: function(selImg) {
            var me = this;
            if (!selImg.ntpl) {
                selImg.ntpl = M.Template(me.tpls.paperProp);
            }

            $.ajax({
                cache: false, url: me.urls.getBgImgList,
                dataType: 'JSON',
                type: 'POST',
                success: function(rs) {
                    //rs = eval('(' + rs + ')');
                    me.dcache.smp = rs;
                    me.dcache.selBgId = null;
                    var bgImgUrl = me.dcache.bgImg;
                    if (bgImgUrl) {
                        MT.each(rs, function(v, k) {
                            if (v.url == bgImgUrl) {
                                me.dcache.selBgId = v.id;
                                return false;
                            }
                        });
                    } else {
                        me.dcache.selBgId = '';
                    }
                    selImg.update(selImg.ntpl.render(me.dcache));

                    var node = me.ptSetting.node;
                    $(node.tag).find('input[name="colorSel"]').colorpicker({
                        fillcolor:true,
                        success:function(o, color){
                            $(o).css("background-color", color);
                        }
                    }, me);
                }
            });
        },
        getSetting: function(o, e) {
            var me = this, node = me.ptSetting.node, $node = $(node.tag),
                sel = $node.find("select[name='selImgs'] option:selected"),
                id = sel.val(), dt = me.dcache.smp || [], st = {}, w, h, c;
            MT.each(dt, function(v, k) {
                if (v.id == id) {
                    st.bgImg = (v.url || '').replace(/\\/g, "/");
                    return false;
                }
            });
            w = parseInt($node.find('input[name="paperW"]').val(), 10);
            h = parseInt($node.find('input[name="paperH"]').val(), 10);
            if (MT.isNumber(w) && w > 100) st.width = w;
            if (MT.isNumber(h) && h > 100) st.height = h;
            c = $node.find('input[name="colorSel"]').css("background-color");
            st.bgColor = c || '#fff';
            if (st.bgColor == 'transparent') st.bgColor = '#fff';
            me.paperCfg(st);
        },
        initPaper: function() {
            var me = this,
                panel = me.ct,
                pbody = panel.childCt.body,
                size = {width: 800, height: 600},
                paperct = $("#_nsubwayct");

            me.ctbody = pbody;
            me.offsets = pbody.getXY();
            me.canDraw = false;
            me.isEdit = false;
            me.txtEdit = false; // now in text edit?
            me.txtInputObj = null; // the txt line input to set style?
            me.editState = false;
            me.drawLine = false; // draw line
            me.tmpLine = {};
            me.delKeyEnabled = true;
            me.lineIn = {};
            me.orgCoor = [0, 0];
            me.paperct = paperct = MT.get(paperct[0]);
            me.dcache = {
                bgColor: '#fff',
                width: size.width,
                height: size.height,
                size: size // for create
            };
            me.paper = Raphael(paperct.tag, size.width, size.height); // init paper object
            me.paperCfg(); // set default config, eg: size & bgColr
            me.lineDotSet = me.paper.set();
            me.bindKeyNavForNode();
            me.bindEvents();
            me.paper.doLine.init({host: me}); // init line
            paperct.on('mousedown', MT.func.bind(me.onMouseDown, me));
            paperct.on('contextmenu', function(e) { e.halt(); });
            MT.get(document).on('keyup', MT.func.bind(me.onKeyUp, me));
            panel.unmask();
        },
        onKeyUp: function(e) {
            this.ctrlKeyDn = false;
        },
        resetPaper: function() {
            if (confirm("该操作将清空当前所有编辑数据，确定操作？")) {
                var me = this, dcache = me.dcache,
                    paperct = me.paperct, w = dcache.size.width, h = dcache.size.height;
                me.resetData();
                me.paperCfg({width: w, height: h, bgColor: '#ffffff'});
                me.dcache.nowData = null;
                if (me.paper) {
                    me.paper.clear();
                    me.paper.setSize(w, h);
                }
                me.changeTitle();
            }
        },
        changeTitle: function() {
            var me = this, nowData = me.dcache.nowData, title = "新建";
            if (nowData && nowData.name) {
                title = nowData.name;
            }
            title = me.title + ' —— 当前: <span style="color:#0000ff">' + title + '</span>';
            me.ct.setTitle(title);
        },
        resetData: function() {
            var me = this;
            me.nodes = {}; // reset data
            me.relTree = {};
            me.unTrans();
            me.lines = {};
            me.paper && me.paper.clear();
        },
        setBgImg: function() { // 为画布添加图片背景。
            var me = this,
                oimg = me.dcache.bgImg;
            oimg && oimg.length && me.paperct.setStyle("background-image:url("+oimg+");background-repeat:no-repeat;background-position:0 0");
        },
        openEx: function() { // open
            var me = this;

            if (!me.selEx) {
                me.selEx = new MT.ui.Panel({
                    renderTo: document.body,
                    noHeader: true,
                    width: 300,
                    height: 300,
                    closeAction: 'hide',
                    floating: true, bodyCls: 'pnebody',
                    bodyPadding: 5, autoScroll: true,
                    listeners: {
                        'show': function(w) {
                            me.getEx(w);
                        }
                    },
                    items: [
                        {
                            mType: 'toolbar',
                            dock: 'bottom',
                            items: [
                                {
                                    mType: 'button',
                                    pack: 'right',
                                    label: '确 定',
                                    handler: function(o, e) {
                                        me.getExData(o, e);
                                        me.selEx.hide();
                                    }
                                },
                                {
                                    mType: 'button',
                                    pack: 'right',
                                    label: '取 消',
                                    handler: function(o, e) {
                                        me.selEx.hide();
                                    }
                                }
                            ]
                        }
                    ]
                });
            }
            me.selEx.setPagePosition(me.selEx.node.getAlignToXY(MT.CompMgr.get(editorMap[22]).node, 'tr-br?'));
            me.selEx.show();
        },
        getEx: function(selImg) {
            var me = this;
            if (!selImg.ntpl) {
                selImg.ntpl = M.Template(me.tpls.exList);
            }
            $.ajax({
                cache: false, url: me.urls.getData,
                dataType: 'JSON',
                success: function(rs) {
                    //rs = eval('(' + rs + ')');
                    selImg.update(selImg.ntpl.render({data: rs}));
                }
            });
        },
        getExData: function(o, e) {
            var me = this, node = me.selEx.node,
                sel = $(node.tag).find("input[name='selGraph']:checked"),
                uuid = sel.val(), name = sel.parent().find('label').text();

            if (me.editState) {
                if (confirm("是否先保存当前正在编辑的数据？")) {
                    me.editFileName();
                } else {
                    me.doLoadData_(uuid, name);
                }
            } else {
                me.doLoadData_(uuid, name);
            }
        },
        doLoadData_: function(id, name) {
            var me = this;
            me.dcache.nowData = {uuid: id, name: name};
            me.loadData(id);
            me.changeTitle();
        },
        onMouseDown: function(e) {
            var me = this, scrl, offsets,
                tagName = e.target.tagName.toLowerCase();

            if (tagName == 'svg' || tagName == 'div') {
                me.unTrans();
                delete me.nowNode;
            }
            if (!me.canDraw) return;
            offsets = me.offsets;
            e.halt();
            scrl = me.ctbody.getScroll();
            me.orgCoor = [e.pageX - offsets.x + scrl.left, e.pageY - offsets.y + scrl.top];
            if (me.drawLine) {
                me.paperct.on('mousemove', MT.func.bind(me.onMouseMove, me));
                me.paperct.on('mouseup', MT.func.bind(me.onMouseUp, me));
                me.tmpLine = {from: {x: me.orgCoor[0], y: me.orgCoor[1]}};
                if (me.lineIn.pos && me.lineIn.pos != 'x') {
                    me.tmpLine.ftnode = {sid: me.lineIn.id, pos: me.lineIn.pos};
                    me.lineIn.pos = 'x';
                }
                return;
            }
            me.drawNode(e);
        },
        onMouseMove: function(e) {
            var me = this,
                lastCoor, scrl = me.ctbody.getScroll(), tmpLine = me.tmpLine,
                from = tmpLine.from, dx, dy;

            lastCoor = [e.pageX - me.offsets.x + scrl.left, e.pageY - me.offsets.y + scrl.top];
            tmpLine['to'] = {x: lastCoor[0], y: lastCoor[1]};
            if (!tmpLine.line) {
                tmpLine.line = me.paper.path('').attr({"stroke-dasharray": "--"});
            }
            if (me.defV.lineType === 2 && e.shiftKey) {
                dx = Math.abs(lastCoor[0] - from.x);
                dy = Math.abs(lastCoor[1] - from.y);
                if (dx > dy) { // horizontal
                    lastCoor[1] = from.y;
                    tmpLine['to'].y = from.y;
                } else {
                    lastCoor[0] = from.x;
                    tmpLine['to'].x = from.x;
                }
            }
            tmpLine.line.attr({path: ['M', from.x, from.y, 'L', lastCoor[0], lastCoor[1]]});
        },
        onMouseUp: function(e) {
            var me = this,
                tmpLine = me.tmpLine,
                id = MT.guid('line_'+ uuid), fid = 'x', tid = 'x', ftnode, dirs = {from:'n',to:'s'},
                from, to, paper = me.paper;
            if (tmpLine) {
                from = tmpLine.from;
                to = tmpLine.to || {x:0,y:0};
                if (me.defV.lineType === 1) { // check direction for broken line
                    dirs = me.checkDirect(from.x, from.y, to.x, to.y);
                }
                ftnode = tmpLine.ftnode;
                if (ftnode) {
                    fid = ftnode.sid;
                    dirs.from = ftnode.pos;
                    from = paper.doLine.getJunctionXY(dirs.from, me.nodes[fid].node.getBBox());
                }
                if (me.lineIn.pos && me.lineIn.pos != 'x') {
                    tid = me.lineIn.id;
                    dirs.to = me.lineIn.pos;
                    me.lineIn.pos = 'x';
                    to = paper.doLine.getJunctionXY(dirs.to, me.nodes[tid].node.getBBox());
                }
                me.renderLine(id, {from: fid, to: tid}, from, to, dirs, me.defV.lineType, null, null, null);
                tmpLine.line && tmpLine.line.remove();
                me.tmpLine = undefined;
                me.editState = true;
                //me.removeLineDot();
            }
            me.paperct.off('mousemove');
            me.paperct.off('mouseup');
        },
        renderLine: function(id, rels, fCoor, toCoor, dirs, type, arrPos, arrType, style) {
            var me = this, line, paper = me.paper;
            me.lines[id] = {
                from: rels.from, fCoor: fCoor, fdir: dirs?dirs.from:null,
                to: rels.to, tCoor: toCoor, tdir: dirs?dirs.to:null,
                path: null,
                type: type,
                arrPos: arrPos,
                arrType: arrType
            };
            if (type === 1) {
                line = paper.path(paper.doLine.renderEdge(me.lines[id]));
            } else {
                line = paper.path(['M', fCoor.x, fCoor.y, 'L', toCoor.x, toCoor.y]);
            }
            line.id = id;
            style && line.attr(style);
            if (arrPos && arrPos != "") {
                me.makeArrow(id, line, arrPos + '-' + arrType);
            }
            me.addNode(line, 'line', null, null, null);
            me.bindMouseDn(line);
            me.bindDblClk(line);
            me._selNode(line);
            me.addLineRels(id, rels.from, rels.to);
            return line;
        },
        checkDirect: function(x1, y1, x2, y2) { // check direction for broken line
            var me = this,
                dx = Math.abs(x2-x1),
                dy = Math.abs(y2-y1), from, to;
            if (dy <= dx) {
                if (x2 >= x1) {
                    from = 'e';
                    to = 'w';
                } else {
                    from = 'w';
                    to = 'e';
                }
            } else {
                if (y2 >= y1) {
                    from = 's';
                    to = 'n';
                } else {
                    from = 'n';
                    to = 's';
                }
            }
            return {from: from, to: to};
        },
        updateLineDirect: function(lineId) {
            var me = this,
                line = me.lines[lineId], fc = line.fCoor, tc = line.tCoor,
                dirs;
            dirs = me.checkDirect(fc.x, fc.y, tc.x, tc.y);
            MT.mixin(me.lines[lineId], {
                fdir: dirs.from,
                tdir: dirs.to
            }, true);
        },
        addLineRels: function(lineId, from, to) { // add line link to shape.
            var me = this;
            if (from != 'x') {
                if (me.checkLineIds_(from)) {
                    me.nodes[from].lineIds.push({id: lineId, ft: 'f'});
                }
            }
            if (to != 'x') {
                if (me.checkLineIds_(to)) {
                    me.nodes[to].lineIds.push({id: lineId, ft: 't'});
                }
            }
        },
        removeLineRels: function(lineId, from, to) { // remove line link from shape.
            var me = this, obj, idx = -1;
            if (from != 'x') {
                if (me.nodes[from] && (obj=me.nodes[from].lineIds)) {
                    idx = MT.array.findIndex(obj||[], function(o, i) {
                        if (o.id == lineId) return true;
                    });
                    if (idx != -1) MT.array.removeAt(obj, idx);
                }
            }
            if (to != 'x') {
                idx = -1;
                if (me.nodes[to] && (obj=me.nodes[to].lineIds)) {
                    idx = MT.array.findIndex(obj||[], function(o, i) {
                        if (o.id == lineId) return true;
                    });
                    if (idx != -1) MT.array.removeAt(obj, idx);
                }
            }
        },
        updateLineRels: function(lineId, newfrom, newto, oldfrom, oldto) {
            var me = this;
            if (newfrom != oldfrom) {
                me.removeLineRels(lineId, oldfrom, 'x');
                me.addLineRels(lineId, newfrom, 'x');
            }
            if (newto != oldto) {
                me.removeLineRels(lineId, 'x', oldto);
                me.addLineRels(lineId, 'x', newto);
            }
        },
        checkLineIds_: function(id) {
            var me = this, nd;
            if ((nd=me.nodes[id])) {
                if (!nd.lineIds) nd.lineIds = [];
                return true;
            }
            return false;
        },
        toDraw: function(type, obj) {
            var me = this;
            me.drawType = type;
            me.nowNode = null;

            if (type !== 'move' || !obj.pressed) {
                me.unTrans();
            }
            if (type === 'move') {
                me.canDraw = false;
                me.drawLine = false;
                me.paperct.removeClass('crosshair');
                if (!obj.pressed) me.drawType = null;
            } else {
                if (obj.pressed) {
                    me.canDraw = true;
                    me.drawLine = (type == 'edoline' || type == 'edoSline') ? true : false;
                } else {
                    me.canDraw = false;
                    me.drawType = null;
                    me.drawLine = false;
                }
                me.paperct[obj.pressed ? 'addClass' : 'removeClass']('crosshair');
            }
            me.textInpt && me.textInpt.hide();
            me.removeLineDot();
        },
        drawNode: function(e) {
            var me = this,
                args = [], drawType = me.drawType,
                orgCoor = me.orgCoor, paper = me.paper;

            if (drawType === 'text') { // text node, special.
                me.showTextA(e.pageX, e.pageY, null);
                return;
            }
            switch (drawType) {
                case "circle":
                    args = [orgCoor[0], orgCoor[1], 15];
                    break;
                case "rect":
                    args = [orgCoor[0], orgCoor[1], 50, 25];
                    break;
                case "ellipse":
                    args = [orgCoor[0], orgCoor[1], 20, 10];
                    break;
                case "rectc":
                    drawType = 'rectc';
                    args = [orgCoor[0], orgCoor[1], 20, 50, 10];
                    break;
                case "rhombus":
                    args = [orgCoor[0], orgCoor[1], 50, 30];
                    break;
                case "triangle":
                    args = [orgCoor[0], orgCoor[1], 30, 30];
                    break;
                case "rects":
                    args = [orgCoor[0], orgCoor[1], 50, 25];
                    break;
                case "hexagon":
                    args = [orgCoor[0], orgCoor[1], 50, 40, 8];
                    break;
            }
            me.renderShape(null, drawType, args, null, null);
            me.editState = true;
        },
        renderShape: function(id, drawType, args, style, rotateDig, hLink) {
            var me = this,
                nowNode, paper = me.paper, dtp,
                keepRatio = false, rotate = false/*['axisX', 'axisY']*/;

            if (drawType === 'circle') {
                keepRatio = ['axisX', 'axisY', 'bboxCorners', 'bboxSides'];
                rotate = false;
            }
            dtp = drawType=='rectc'?'rect':drawType;
            if (MT.isFunction(paper[dtp])) {
                nowNode = paper[dtp].apply(paper, args);

                nowNode.attr(style || {fill: "#fff", stroke: "#000"});
                nowNode.id = id || MT.guid('shape_' + uuid);
                if (MT.isNumber(rotateDig)) nowNode.rotate(rotateDig);
                me.addNode(nowNode, drawType, keepRatio, rotate);
                if (hLink) me.nodes[nowNode.id].hLink = hLink;
                me.bindMouseDn(nowNode);
                me.bindDblClk(nowNode);
                me.bindMouseOverOut(nowNode);
                me._selNode(nowNode);
                return nowNode.id;
            }
            return id;
        },
        bindMouseOverOut: function(nowNode) {
            var me = this;
            nowNode.mouseover(function() {
                me.removeLineDot();
                me.onDotMouseEnter(this);
            });
        },
        onDotMouseEnter: function(nowNode) {
            var me = this,
                i,
                paper = me.paper, dot, bb, xy;

            if (me.drawLine && me.lineDotSet.length === 0) {
                me.lineIn.id = nowNode.id;
                bb = nowNode.getBBox();
                MT.each(['n', 's', 'e', 'w'], function(v, k) {
                    xy = paper.doLine.getJunctionXY(v, bb);
                    dot = paper.circle(xy.x, xy.y, 3).attr({fill: '#cfe4fa'});
                    dot.id = v + k;
                    dot.mouseover(function() {
                        me.lineIn.pos = String(this.id).charAt(0);
                        this.attr({fill: '#ff0000', r: 6});
                    }).mouseout(function() {
                        me.lineIn.pos = 'x';
                        this.attr({fill: '#cfe4fa', r: 3});
                    });
                    me.lineDotSet.push(dot);
                });
            }
        },
        removeLineDot: function() {
            var me = this;
            if (me.lineDotSet.length != 0) {
                me.lineDotSet.unmouseover();
                me.lineDotSet.unmouseout();
                me.lineDotSet.remove();
                me.lineDotSet.clear();
            }
        },
        addNode: function(nowNode, drawType, keepRatio, rotate, scale) {
            var me = this, ft, _d = ['self'];
            //me.nowNode = nowNode;
            if (drawType == 'text') _d.push('center');
            scale = scale != undefined ? scale : ['bboxCorners', 'bboxSides'];
            me.nodes[nowNode.id] = {node: nowNode, drawType: drawType, ft: {keepRatio: keepRatio, draw: ["bbox"], size: 3, scale: scale, drag: _d, rotate: rotate, attrs: {fill:"#cfe4fa",stroke:"#1b61a9"}}, idx: me.index++};
        },
        doCopyNode: function(src, dx, dy, pid) {
            var me = this, paper = me.paper, renderArg,
                ndObj = me.nodes[src.id], attrs, transform, bb, x, y, t, tx, ty, _pid;
            me.nowNode = null;
            transform = (src.type == 'set' ? src[0] : src).attr('transform');
            t = transform[2];
            if (t) {
                tx = t[1] || 0;
                ty = t[2] || 0;
            }
            bb = src.getBBox();
            x = bb.x + dx;
            y = bb.y + dy;
            if (src.isText) {
                renderArg = [
                    ndObj.txtData,
                    null, {x: x, y: y}, transform[0] && transform[0][1] ? transform[0][1] : '',
                    pid
                ];
                me.doRenderText.apply(me, renderArg);
            } else {
                attrs = {};
                MT.each(src.attrs, function(v, k) {
                    if (shapeProp[k]) {
                        attrs[k] = v;
                    }
                });
                renderArg = paper.shapeType(ndObj.drawType, MT.mixin({x: x, y: y}, bb));
                _pid = me.renderShape.apply(me, [null, ndObj.drawType, renderArg, attrs, null, ndObj.hLink]);
                var prel = me.relTree[src.id];
                if (prel && prel.cid && prel.cid.length) {
                    me.doCopyNode(me.nodes[prel.cid[0]].node, dx, dy, _pid);
                }
            }
        },
        updatePos: function(src, dx, dy) {
            var me = this, id = src.id, prel = me.relTree[id], cnode;
            if (prel && prel.cid && prel.cid.length) { // update text pos
                cnode = me.nodes[prel.cid[0]];
                cnode && cnode.node && cnode.node.translate(dx, dy);
            }
            if (src.isText) return;
            // update line link to the shape.
            var ndObj = me.nodes[id], lineIds = ndObj.lineIds || [];
            MT.each(lineIds, function(o) {
                me.updateLinePos(src, o.id, o.ft);
            });
        },
        updateLinePos: function(src, lineId, ft) {
            var me = this, line = me.lines[lineId],
                paper = me.paper, xy, direct, ol;
            if (line) {
                ol = me.nodes[lineId].node;
                if (ft == 'f') {
                    direct = line.fdir;
                } else if (ft == 't') {
                    direct = line.tdir;
                }
                xy = paper.doLine.getJunctionXY(direct, src.getBBox());
                line[ft == 'f' ? 'fCoor' : 'tCoor'] = xy;

                if (line.arrPos && line.arrPos != "") {
                    line.arr1 && line.arr1.remove();
                    line.arr2 && line.arr2.remove();
                }

                if (line.type === 1) {
                    ol.attr({path: paper.doLine.renderEdge(line)});
                } else {
                    ol.attr({path: ['M', line['fCoor'].x, line['fCoor'].y, 'L', line['tCoor'].x, line['tCoor'].y]});
                }

                if (line.arrPos && line.arrPos != '') {
                    me.makeArrow(lineId, ol, (line.arrPos||'') + '-' +(line.arrType||""));
                }
            }
        },
        bindMouseDn: function(nowNode) {
            var me = this;
            nowNode.mousedown(function(ev) {
                me.nowid = nowNode.id;
                me.nowNode = nowNode;
                if (me.drawType !== 'move') return false;
                me.removeLineDot();
                me.doFreeTransf(nowNode);
                //ev.preventDefault();
                //ev.stopPropagation();
            });
        },
        bindKeyNavForNode: function() {
            var me = this;
            new M.util.keyMap({
                key: [37, 38, 39, 40, 46, 86, 17],
                fn: function(key, e) {
                    if (key == 17) {
                        me.ctrlKeyDn = true;
                    }
                    if (me.drawType !== 'move' && key != 86) return false;
                    if (key == 46) { // delete node
                        this.deleteNode();
                    } else if (key == 86 && me.delKeyEnabled){ // v char, for move node
                        if (!me.txtEdit) {
                            var btnMove = MT.CompMgr.get('btn_moveNode');
                            btnMove.toggle(true);
                            me.toDraw('move', btnMove);
                        }
                    }
                },
                scope: me
            });
        },
        _selNode: function(nowNode) {
            var me = this,
                btnMove = MT.CompMgr.get('btn_moveNode');
            me.nowid = nowNode.id;
            me.nowNode = nowNode;
            btnMove.toggle(true);
            me.toDraw('move', btnMove);
            me.removeLineDot();
            me.doFreeTransf(nowNode);
        },
        deleteNode: function(node) {
            var me = this,
                nowNode = node || me.nowNode,
                id, relT, pt;
            if (!me.delKeyEnabled) return;
            me.unTrans();
            if (nowNode) {
                id = nowNode.id;
                if ((relT=me.relTree[id])) {
                    if (relT.pid) { // remove from parent
                        pt = me.relTree[relT.pid];
                        pt && MT.array.remove(pt.cid || [], id);
                    }
                    if (relT.cid && relT.cid.length) { // remove child
                        MT.each(relT.cid, function(v, k) {
                            me.nodes[v] && me.deleteNode(me.nodes[v].node);
                        });
                    }
                }
                !nowNode.isText && MT.off(nowNode.node);
                nowNode.remove();
                me.nodes[id] = undefined;
                delete me.nodes[id];
                if (String(id).indexOf('line') != -1) {
                    var line = me.lines[id];
                    line.arr1 && line.arr1.remove();
                    line.arr2 && line.arr2.remove();
                    me.removeLineRels(id, line.from, line.to);
                    me.lines[id] = undefined;
                    delete me.lines[id];
                }
                me.nowNode = null;
            }
        },
        doFreeTransf: function(node) {
            var me = this, id = node.id, nowTranf = me.nowTranf;
            if (me.drawType === 'move') {
                if (nowTranf.id != id) {
                    me.unTrans();
                    if (String(id).indexOf('line') != -1) {
                        nowTranf.ft = me.paper.lineTrans(me, node, me.nodes[id].ft);
                    } else {
                        nowTranf.ft = me.paper.freeTransform(me, node, me.nodes[id].ft);
                    }
                    nowTranf.id = id;
                }
            }
        },
        unTrans: function() {
            var me = this, nowTranf = me.nowTranf;
            if (nowTranf.ft) {
                nowTranf.id = null;
                nowTranf.ft.unplug();
            }
            // hide edit panel
            if (me.isEdit) {
                me.isEdit = false;
                me.textInpt && me.textInpt.hide();
            }
        },
        showTextA: function(x, y) {
            var me = this;
            if (!me.textInpt) {
                me.textInpt = new MT.ui.Window({
                    renderTo: document.body,
                    width: 250, height: 250,compCls: 'ltcls',
                    autoScroll: true,
                    closeAction: 'hide',
                    listeners: {
                        "show": function() {
                            me.renderTxts(this);
                        },
                        "hide": function() {
                            me.txtEdit = false;
                            me.delKeyEnabled = true;
                            me.txtInputObj = null;
                            me.toggleTxtBtn(true); // disable text class
                        }
                    },
                    items: [
                        {
                            mType: 'toolbar',
                            dock: 'bottom',
                            items: [
                                {
                                    mType: 'button',
                                    pack: 'right',
                                    label: '确 定',
                                    handler: function(o, e) {
                                        me.textInpt.hide();
                                        me.getTxts();
                                    }
                                },
                                {
                                    mType: 'button',
                                    pack: 'right',
                                    label: '取 消',
                                    handler: function(o, e) {
                                        me.textInpt.hide();
                                    }
                                }
                            ]
                        }
                    ]
                });
                if (!me.textInpt.ntpl) {
                    me.textInpt.ntpl = MT.Template(me.tpls.txtM);
                }
            }
            me.txtEdit = true;
            me.delKeyEnabled = false;
            var vw = MT.dom.getViewportWidth(), vh = MT.dom.getViewportHeight();
            if (x + 250 > vw) x = vw - 255;
            if (y + 250 > vh) y = vh - 255;
            me.textInpt.showAt(x, y);
        },
        renderTxts: function(textPne) {
            var me = this,
                nowNode = me.nowNode, id,
                tmp = {"txts": [{"style": "text-align:center", "text": ""}]},
                data,
                tpl,
                body = textPne.childCt.body, rel = me.relTree[id], cnode;
            if (nowNode) {
                id = nowNode.id;
                data = me.nodes[id]["txtData"];
                rel = me.relTree[id];
                if (!nowNode.isText && rel && rel.cid && rel.cid.length) {
                    MT.each(rel.cid, function(cid) {
                        cnode = me.nodes[cid];
                        if (cnode && cnode.node && cnode.node.isText) {
                            data = cnode.txtData;
                            return false;
                        }
                    });
                }
            }
            tpl = textPne.ntpl.render(data || tmp);
            textPne.update(tpl);
            me.toggleTxtBtn(false); // enable text class
            body.off('click');
            body.on('click', '.sm-removerow', function(e) {
                e.halt();
                $(this).parent().parent().remove();
            });
            body.on('click', '.sm-addrow', function(e) {
                e.halt();
                $(this).prev().append('<tr><td><input type="text" style="text-align:center;" class="txtinpt" value="" /></td><td><a href="javascript:void(0);" class="sm-removerow">—</a></td></tr>');
            });
            body.on('click', '.txtinpt', function(e) {
                e.halt();
                me.txtInputObj = this;
            });
        },
        getTxts: function() {
            var me = this,
                txtpne = me.textInpt,
                bodytag = txtpne.childCt.body.tag, rs = [], _style;
            $(bodytag).find("input.txtinpt").each(function() {
                _style = $(this).attr("style") || "";
                rs.push({"style": _style.toLowerCase(), "text": $.trim(this.value)});
            });
            me.editState = true;
            me._editTxt = true;
            me.doRenderText({"txts": rs}, false, false);
        },
        doRenderText: function(txtData, id, xy, rotateDig, pid, hLink) {
            var me = this, orgCoor = me.orgCoor, st, text, nowNode = me.nowNode,
                bb = {x: orgCoor[0], y: orgCoor[1], width: 0, height: 0},
                paper = me.paper, cnode, rel, isNew = true;
            if (nowNode) {
                bb = nowNode.getBBox();
                if (nowNode.type == 'set' && nowNode.isText) { // text node
                    st = nowNode;
                    nowNode.remove();
                    nowNode.clear();
                } else {
                    rel = me.relTree[nowNode.id];
                    if (rel && rel.cid && rel.cid.length) {
                        MT.each(rel.cid, function(cid) {
                            cnode = me.nodes[cid];
                            if (cnode && cnode.node && cnode.node.isText) {
                                st = cnode.node;
                                bb = st.getBBox();
                                st.remove(); // clear old
                                st.clear();
                                isNew = false;
                                return false;
                            }
                        });
                    }
                }
            }
            bb = MT.isObject(xy) ? xy : bb;
            st = st || paper.set();
            st.isText = true;
            st.id = id || st.id || MT.guid("st_" + uuid);
            me.addNode(st, 'text', true, false/*['axisX']*/, false);
            me.tuneText(st, txtData.txts, bb);
            me.nodes[st.id].txtData = txtData; // save txtData for edit.
            if (hLink) me.nodes[st.id].hLink = hLink;
            if (rotateDig && MT.isNumber(rotateDig)) st.rotate(rotateDig);
            if (pid || (isNew && nowNode && !nowNode.isText)) { // add text for shape
                me.addRelTree(pid||nowNode.id, st.id);
            }
            st.attr({cursor: 'default'});
            me.bindMouseDn(st);
            me.bindDblClk(st);
            me._selNode(st);
            me._editTxt = false;
            return st.id;
        },
        addRelTree: function(pid, cid) { // add to relTree
            var me = this,
                prel = me.relTree[pid],
                crel = me.relTree[cid];
            me.relTree[pid] = prel = prel || {};
            prel.cid = prel.cid || [];
            if (MT.array.indexOf(cid, prel.cid) == -1) {
                prel.cid.push(cid);
            }
            me.relTree[cid] = crel = crel || {};
            crel.cid = crel.cid || [];
            crel.pid = pid;
        },
        tuneText: function(st, texts, xy) {
            var me = this,
                fontSize = 12/*me.defV.size*/, setId = st.id,
                leading = 1.5,
                align = {'left': 'start', 'center': 'middle', 'right': 'end'},
                obj, x = xy.x, y = xy.y, dy = 0,
                txt, styles, str, k, v, fill, txtAnchor, flag = false, sfsize,
                hw = xy.width/2||0, hh = xy.height||0, theText = "", dsize, realText;
            if (me._editTxt) y -= 1;
            for (var i = 0, ii = texts.length; i < ii; i++) {
                txt = texts[i];
                str = "";
                txtAnchor = "";
                fill = "";

                styles = MT.string.trim(txt.style).split(/\s*(?::|;)\s*/);
                sfsize = null;
                for (var j = 0, len = styles.length; j < len;) {
                    k = MT.string.toCamelCase(styles[j++]);
                    v = styles[j++] || '';
                    flag = false;
                    if (k == 'color') {
                        fill = v; flag = true;
                    }
                    if (k == 'textAlign') {
                        k = "text-anchor";
                        v = align[v];
                        txtAnchor = v || "";
                    }
                    if (k == 'fontSize') sfsize = parseInt(v.replace('px', ''), 10);
                    if (!flag && k != '') str += k + ':' + v + ';';
                }
                sfsize = sfsize || fontSize;
                dsize = sfsize * leading;
                dy += i > 0 ? dsize : dsize/2;
                realText = txt.text;
                obj = me.paper.text(x+hw, y + hh + dy, realText == '' ? '　' : realText);
                obj.attr({"font-size": sfsize});
                str != "" && MT.dom.setStyle(obj.node, str);
                if (fill != '') obj.attr({fill: fill});
                if (txtAnchor != '') obj.attr({"text-anchor": txtAnchor});
                obj.setId = setId;
                st.push(obj);
                theText += txt.text + '&#xa;';
            }
            var bb = st.getBBox(), bw = bb.width, nanc;
            MT.each(st, function(nd, i) {
                nanc = nd.attr('text-anchor');
                switch (nanc) {
                    case 'start':
                        nd.attr({x: x});
                        break;
                    case 'end':
                        nd.attr({x: x+bw});
                        break;
                    default:
                        nd.attr({x: x+bw/2});
                }
            });
            me.nodes[setId].txt = theText.replace(/&#xa;$/g, '');
        },
        toggleTxtBtn: function(flag) {
            var me = this,
                toglist = [1, 2, 3, 4, 5, 6, 7, 8, 9];
            MT.each(toglist, function(v, i) {
                MT.CompMgr.get(editorMap[v]).setDisabled(flag);
            });
        },
        bindDblClk: function(node) {
            var me = this;
            node.dblclick(function() {
                me.nowNode = node;
                me.isEdit = true;
                var bb = node.getBBox(), offsets = me.offsets, scrls = me.ct.childCt.body.getScroll();
                me.showTextA(offsets.x + bb.x - scrls.left, offsets.y + bb.y - scrls.top);
            });
        },
        showDiyPne: function(title, type) {
            var me = this;
            if (!me.diyPne) {
                me.diyPne = new MT.ui.Window({
                    renderTo: document.body,
                    width: 150, height: 90, title: '自定义',
                    html: '<div class="diyset"><input type="text" name="diyset" /></div>',
                    closeAction: 'hide', visible: true,
                    listeners: {
                        "show": function() {
                            $(this.node.tag).find('input[name="diyset"]').val('').focus();
                        }
                    },
                    items: [
                        {
                            mType: 'toolbar',
                            dock: 'bottom',
                            items: [
                                {
                                    mType: 'button',
                                    pack: 'right',
                                    label: '确 定',
                                    handler: function(o, e) {
                                        me.diyPne.hide();
                                        me.getDiy();
                                    }
                                },
                                {
                                    mType: 'button',
                                    pack: 'right',
                                    label: '取 消',
                                    handler: function(o, e) {
                                        me.diyPne.hide();
                                    }
                                }
                            ]
                        }
                    ]
                });
            }
            me.diyPne.diyType = type;
            me.diyPne.setTitle(title);
            me.diyPne.center().show();
        },
        getDiy: function() {
            var me = this,
                diypne = me.diyPne,
                bodytag = diypne.childCt.body.tag, diyVal;
            diyVal = $(bodytag).find('input[name="diyset"]').val();
            switch (diypne.diyType) {
                case 'diyfont':
                    me.defV.family = diyVal;
                    MT.CompMgr.get(editorMap[1]).setLabel(diyVal);
                    MT.fire(me, 'fontChange');
                    break;
                case 'diyfontsize':
                    diyVal = MT.number.parse(diyVal, 12);
                    me.defV.size = parseInt(diyVal, 10) || 12;
                    MT.CompMgr.get(editorMap[2]).setLabel(diyVal);
                    MT.fire(me, 'fontSizeChange');
                    break;
                case 'diylinesize':
                    MT.fire(me, 'lineSizeChange', MT.number.parse(diyVal, 1));
                    break;
            }
        },
        bindEditPne: function(sf) {
            var me = this,
                edt_font = MT.CompMgr.get(editorMap[1]), edt_fontSize = MT.CompMgr.get(editorMap[2]),
                edt_lineType = MT.CompMgr.get(editorMap[12]), edt_lineArr = MT.CompMgr.get(editorMap[15]);

            edt_font.childCt.btnInnerEl.setStyle("width:24px;overflow:hidden");
            MT.CompMgr.get('edt_menu_font').on('click', function(o, it, e) {
                if (!it.diy) {
                    me.defV.family = it.text;
                    edt_font.setLabel(it.text);
                    MT.fire(me, 'fontChange');
                } else {
                    me.showDiyPne('自定义字体', 'diyfont');
                }
            });

            edt_fontSize.childCt.btnInnerEl.setStyle("width:14px;overflow:hidden");
            MT.CompMgr.get('edt_menu_fontSize').on('click', function(o, it, e) {
                if (!it.diy) {
                    var fontsize = it.text.replace('px', '');
                    me.defV.size = parseInt(fontsize, 10) || 12;
                    edt_fontSize.setLabel(fontsize);
                    MT.fire(me, 'fontSizeChange');
                } else {
                    me.showDiyPne('自定义字体大小', 'diyfontsize');
                }
            });

            edt_lineType.on('click', function() {
                me.linePne.setPagePosition(me.linePne.node.getAlignToXY(edt_lineType.node, 'tl-bl?'));
                me.linePne.show();
            }, edt_lineType);

            edt_lineArr.on('click', function() {
                me.lineArrPne.setPagePosition(me.lineArrPne.node.getAlignToXY(edt_lineArr.node, 'tl-bl?'));
                me.lineArrPne.show();
            }, edt_lineArr);

            MT.CompMgr.get('edt_menu_lineSize').on('click', function(o, it, e) {
                if (!it.diy) {
                    MT.fire(me, 'lineSizeChange', it.text);
                } else {
                    me.showDiyPne('自定义线条大小', 'diylinesize');
                }
            });

            $(MT.CompMgr.get(editorMap[3]).node.tag).colorpicker({
                fillcolor:true,
                success:function(o, color){
                    MT.fire(me, 'fontColorChange', color);
                }
            }, me);
            $(MT.CompMgr.get(editorMap[10]).node.tag).colorpicker({
                fillcolor:true,
                success:function(o, color){
                    MT.fire(me, 'lineColorChange', color);
                }
            }, me);
            $(MT.CompMgr.get(editorMap[11]).node.tag).colorpicker({
                fillcolor:true,
                success:function(o, color){
                    MT.fire(me, 'colorChange', color);
                }
            }, me);

            MT.CompMgr.get(editorMap[16]).on('click', function() {
                var nowNode = me.nowNode, relT, tmp;
                if (nowNode) {
                    relT = me.relTree[nowNode.id];
                    if (relT && relT.pid) {
                        me.nodes[relT.pid] && me.nodes[relT.pid].node.toFront();
                    }
                    nowNode.toFront();
                    me.nodes[nowNode.id].idx = me.index++;
                    if (relT && relT.cid) {
                        MT.each(relT.cid||[], function(cid, k) {
                            tmp = me.nodes[cid];
                            if (tmp) {
                                tmp.node.toFront();
                                tmp.idx = me.index++;
                            }
                        });
                    }

                    me.nowTranf.ft && me.nowTranf.ft.updateHandles();
                    me.editState = true;
                }
            });
            MT.CompMgr.get(editorMap[17]).on('click', function() {
                var nowNode = me.nowNode, relT, tmp;
                if (nowNode) {
                    relT=me.relTree[nowNode.id];
                    if (relT && relT.cid) {
                        MT.each(relT.cid||[], function(cid, k) {
                            tmp = me.nodes[cid];
                            if (tmp) {
                                tmp.node.toBack();
                                tmp.idx = -(me.index++);
                            }
                        });
                    }
                    nowNode.toBack();
                    me.nodes[nowNode.id].idx = -(me.index++);
                    if (relT && relT.pid) {
                        tmp = me.nodes[relT.pid];
                        if (tmp) {
                            tmp.node.toBack();
                            tmp.idx = -(me.index++);
                        }
                    }
                    me.nowTranf.ft && me.nowTranf.ft.updateHandles();
                    me.editState = true;
                }
            });

            MT.CompMgr.get(editorMap[4]).on('click', function() {
                MT.fire(me, 'fontBoldChange');
            });
            MT.CompMgr.get(editorMap[5]).on('click', function() {
                MT.fire(me, 'fontItalicChange');
            });
            MT.CompMgr.get(editorMap[6]).on('click', function() {
                MT.fire(me, 'fontUnderlineChange');
            });
            MT.CompMgr.get(editorMap[7]).on('click', function() {
                MT.fire(me, 'txtAlignChange', 'left');
            });
            MT.CompMgr.get(editorMap[8]).on('click', function() {
                MT.fire(me, 'txtAlignChange', 'center');
            });
            MT.CompMgr.get(editorMap[9]).on('click', function() {
                MT.fire(me, 'txtAlignChange', 'right');
            });

            var lineTypeBtn = MT.CompMgr.get(editorMap[14]);
            MT.CompMgr.get('edt_menu_lineType').on('click', function(o, it, e) {
                it.lineType === 1 ? lineTypeBtn.setIconCls('edoline') : lineTypeBtn.setIconCls('edoSline');
                lineTypeBtn.lineType = it.lineType;
                me.defV.lineType = it.lineType;
            });
            lineTypeBtn.on('click', function(o) {
                me.defV.lineType = o.lineType;
                me.toDraw(o.iconCls, o);
            });
        },
        createLinePne: function() {
            var me = this;
            if (!me.linePne) {
                me.linePne = new MT.ui.Panel({
                    renderTo: document.body,
                    width: 200, height:230,
                    html: '<div class="linebox"><ul class="linelist"><li class="line0" tva=""></li><li class="line1" tva="- "></li><li class="line2" tva="--."></li><li class="line3" tva="-"></li><li class="line4" tva="."></li><li class="line5" tva="-."></li><li class="line6" tva="-.."></li><li class="line7" tva=". "></li><li class="line8" tva="--"></li><li class="line9" tva="- ."></li><li class="line10" tva="--.."></li></ul></div>',
                    visible: false,
                    floating: true
                });
                me.linePne.node.on('mouseleave', function() {
                    me.linePne.hide();
                });
                $("ul.linelist>li").click(function() {
                    MT.fire(me, 'lineChange', $(this).attr("tva"));
                    me.linePne.hide();
                });
            }
        },
        createLineArrPne: function() {
            var me = this;
            if (!me.lineArrPne) {
                me.lineArrPne = new MT.ui.Panel({
                    renderTo: document.body,
                    width: 130, height:155,
                    html: '<ul class="arrlist"><li class="arr0" tva="">无箭头</li><li class="arr1" tva="f-1"></li><li class="arr2" tva="t-1"></li><li class="arr3" tva="ft-1"></li><li class="arr4" tva="f-2"></li><li class="arr5" tva="t-2"></li><li class="arr6" tva="ft-2"></li></ul>',
                    visible: false,
                    floating: true
                });
                me.lineArrPne.node.on('mouseleave', function() {
                    me.lineArrPne.hide();
                });
                $("ul.arrlist>li").click(function() {
                    MT.fire(me, 'lineArrChange', $(this).attr("tva"));
                    me.lineArrPne.hide();
                });
            }
        },
        editLink: function() {
            var me = this,
                eLink = MT.CompMgr.get(editorMap[19]);
            if (!me.editUrl) {
                me.editUrl = new MT.ui.Panel({
                    renderTo: document.body,
                    width: 250, height: 100,
                    floating: true, visible: true,
                    listeners: {
                        "show": function() {
                            me.showLink(this);
                        }
                    },
                    items: [
                        {
                            mType: 'toolbar',
                            dock: 'bottom',
                            items: [
                                {
                                    mType: 'button',
                                    pack: 'right',
                                    label: '确 定',
                                    handler: function(o, e) {
                                        me.editUrl.hide();
                                        me.getLink();
                                        me.delKeyEnabled = true;
                                    }
                                },
                                {
                                    mType: 'button',
                                    pack: 'right',
                                    label: '取 消',
                                    handler: function(o, e) {
                                        me.editUrl.hide();
                                        me.delKeyEnabled = true;
                                    }
                                }
                            ]
                        }
                    ]
                });
            }
            me.delKeyEnabled = false;
            me.editUrl.setPagePosition(me.editUrl.node.getAlignToXY(eLink.node, 'tl-bl?'));
            me.editUrl.show();
        },
        showLink: function(obj) {
            var me = this, nowNode = me.nowNode, html, v = "";
            if (nowNode) {
                v = me.nodes[nowNode.id].hLink || "";
            }
            html = '<div style="margin:0 auto;padding:15px 5px;"><p>url:</p><p><input type="text" value="'+v+'" name="linkInpt" class="linkInpt" /></p></div>';
            obj.update(html);
        },
        getLink: function() {
            var me = this, v, inpt, nowNode = me.nowNode;
            inpt = $(me.editUrl.node.tag).find('input[name="linkInpt"]');
            if (nowNode && inpt[0]) {
                v = inpt.val() || "";
                me.nodes[nowNode.id].hLink = v;
            }
        },
        editFileName: function() {
            var me = this,
                eSave = MT.CompMgr.get(editorMap[21]);
            if (!me.editFile) {
                me.editFile = new MT.ui.Panel({
                    renderTo: document.body,
                    width: 250, height: 100, bodyCls: 'pnebody',
                    floating: true, visible: true,
                    listeners: {
                        "show": function() {
                            me.showFileName(this);
                        }
                    },
                    items: [
                        {
                            mType: 'toolbar',
                            dock: 'bottom',
                            items: [
                                {
                                    mType: 'button',
                                    pack: 'right',
                                    label: '确 定',
                                    handler: function(o, e) {
                                        me.getFileName();
                                        me.delKeyEnabled = true;
                                    }
                                },
                                {
                                    mType: 'button',
                                    pack: 'right',
                                    label: '取 消',
                                    handler: function(o, e) {
                                        me.editFile.hide();
                                        me.delKeyEnabled = true;
                                    }
                                }
                            ]
                        }
                    ]
                });
            }
            me.delKeyEnabled = false;
            me.unTrans();
            me.editFile.setPagePosition(me.editFile.node.getAlignToXY(eSave.node, 'tr-br?'));
            me.editFile.show();
        },
        showFileName: function(obj) {
            var me = this, nowData = me.dcache.nowData, html, v = "";
            if (!nowData) {
                me.dcache.nowData = {};
            }
            v = me.dcache.nowData.name || '';
            html = '<div style="margin:0 auto;padding:15px 5px;"><p>要保存的名称:</p><p><input type="text" value="'+v+'" name="edt_fileName" class="linkInpt" /></p></div>';
            obj.update(html);
        },
        getFileName: function() {
            var me = this, v, inpt;
            inpt = $(me.editFile.node.tag).find('input[name="edt_fileName"]');
            if (inpt[0]) {
                v = inpt.val() || "";
            }
            if (!v || v == '') {
                alert("请输入要保存的名称！");
            } else {
                if (!me.dcache.nowData) {
                    me.dcache.nowData = {};
                }
                me.dcache.nowData.name = v;
                me.editFile.hide();
                me.changeTitle();
                me.saveData();
            }
        },
        bindEvents: function() {
            var me = this, func = MT.func;
            MT.on(me, 'lineChange', func.bind(me.onLineChange, me));
            MT.on(me, 'lineArrChange', func.bind(me.onLineArrChange, me));
            MT.on(me, 'fontColorChange', func.bind(me.onFontColorChange, me));
            MT.on(me, 'fontChange', func.bind(me.onFontChange, me));
            MT.on(me, 'fontSizeChange', func.bind(me.onFontSizeChange, me));
            MT.on(me, 'colorChange', func.bind(me.onColorChange, me));
            MT.on(me, 'lineColorChange', func.bind(me.onLineColorChange, me));
            MT.on(me, 'lineSizeChange', func.bind(me.onLineSizeChange, me));
            MT.on(me, 'fontBoldChange', func.bind(me.onFontBoldChange, me));
            MT.on(me, 'fontItalicChange', func.bind(me.onFontItalicChange, me));
            MT.on(me, 'fontUnderlineChange', func.bind(me.onFontUnderlineChange, me));
            MT.on(me, 'txtAlignChange', func.bind(me.onTextAlignChange, me));
        },
        onLineChange: function(d) {
            d = d || "";
            var me = this, node = me.nowNode;
            if (node) node.attr({"stroke-dasharray": d});
            me.editState = true;
        },
        onLineArrChange: function(v) {
            var me = this, node = me.nowNode, id;
            me.defV.lineArr = v || "";
            if (node) {
                id = node.id;
                if (String(id).indexOf('line') != -1) {
                    me.makeArrow(id, node, v||"");
                }
                me.editState = true;
            }
        },
        makeArrow: function(id, node, type) { // make line arrow
            type = type.split('-');
            var me = this,
                dir = type[0] || '', tp = type[1] || '',
                from, to, line = me.lines[id],
                nfill = node.attr('stroke'), sw = node.attr('stroke-width') || 1,
                ang = null, ang1 = null;

            nfill = nfill == 'none' ? '#000' : nfill;

            line.arr1 && line.arr1.remove();
            line.arr2 && line.arr2.remove();
            line['arrPos'] = dir;
            if (type == "") return;
            switch (dir) {
                case '':
                    break;
                case 'f':
                    to = line.fCoor;
                    from = line.tCoor;
                    break;
                case 't':
                    from = line.fCoor;
                    to = line.tCoor;
                    break;
                case 'ft':
                    from = line.fCoor;
                    to = line.tCoor;
                    break;
            }
            line['arrType'] = tp;
            if (line.type === 1) { // broken line
                ang = me.ang[line.fdir];
                ang1 = me.ang[dir == 'f' ? line.fdir : line.tdir];
            }
            line.arr1 = me.paper.drawArr(from, to, tp, nfill, sw, ang1);
            if (dir == 'ft') line.arr2 = me.paper.drawArr(to, from, tp, nfill, sw, ang);
        },
        onColorChange: function(d) {
            d = d || "";
            var me = this, node = me.nowNode;
            if (node) node.attr({"fill": d});
            me.editState = true;
        },
        onFontChange: function() {
            var me = this;
            if (me.txtEdit && me.txtInputObj) {
                me.txtInputObj.style.fontFamily = me.defV.family;
            }
        },
        onFontBoldChange: function() {
            var me = this, fb;
            if (me.txtEdit && me.txtInputObj) {
                fb = me.txtInputObj.style.fontWeight;
                me.txtInputObj.style.fontWeight = fb == 'bold' ? 'normal' : 'bold';
            }
        },
        onFontItalicChange: function() {
            var me = this, fb;
            if (me.txtEdit && me.txtInputObj) {
                fb = me.txtInputObj.style.fontStyle;
                me.txtInputObj.style.fontStyle = fb == 'italic' ? 'normal' : 'italic';
            }
        },
        onFontUnderlineChange: function() {
            var me = this, fb;
            if (me.txtEdit && me.txtInputObj) {
                fb = me.txtInputObj.style.textDecoration;
                me.txtInputObj.style.textDecoration = fb == 'underline' ? '' : 'underline';
            }
        },
        onTextAlignChange: function(anchor) {
            anchor = anchor || "left";
            var me = this;
            if (me.txtEdit && me.txtInputObj) {
                me.txtInputObj.style.textAlign = anchor;
            }
        },
        onFontSizeChange: function() {
            var me = this;
            if (me.txtEdit && me.txtInputObj) {
                me.txtInputObj.style.fontSize = me.defV.size + 'px';
            }
        },
        onFontColorChange: function(d) {
            d = d || "";
            var me = this;
            if (me.txtEdit && me.txtInputObj) {
                me.txtInputObj.style.color = d;
            }
        },
        onLineColorChange: function(d) {
            d = d || "";
            var me = this, node = me.nowNode, id;
            if (node) {
                node.attr({"stroke": d});
                id = node.id;
                if (String(id).indexOf('line') != -1) {
                    var line = me.lines[id];
                    line.arr1 && line.arr1.attr({"fill": d, "stroke": d});
                    line.arr2 && line.arr2.attr({"fill": d, "stroke": d});
                }
                me.editState = true;
            }
        },
        onLineSizeChange: function(d) {
            d = d || 1;
            var me = this, node = me.nowNode;
            if (node) {
                node.attr({"stroke-width": d});
                id = node.id;
                if (String(id).indexOf('line') != -1) {
                    var line = me.lines[id];
                    line.arr1 && line.arr1.attr({"stroke-width": d});
                    line.arr2 && line.arr2.attr({"stroke-width": d});
                }
                me.editState = true;
            }
        },
        getTextTrans: function(txtSet) {
        },
        saveData: function() { // save data
            this.ct.mask("数据保存中，请稍后...");
            var me = this, dcache = me.dcache,
                node, nodes, drawType,
                fill, stroke, strokewidth, stroke_dasharray, bb,
                attrs, saveFlag = false, out, nowData,
                data, paperct = me.paperct, paperSize = paperct.getSize();

            nowData = dcache.nowData || {};

            data = {
                uuid: nowData.uuid || '',
                bgImg: dcache.bgImg || '',
                bgColor: dcache.bgColor || '#fff',
                width: dcache.width,
                height: dcache.height,
                shapes: [],
                lines: [],
                texts: []
            };

            nodes = me.nodes;
            MT.each(nodes, function(v, key) {
                saveFlag = true;
                drawType = v.drawType;
                node = v.node;
                if (drawType != 'text') {
                    fill = node.attr('fill');
                    stroke = node.attr('stroke');
                    strokewidth = node.attr('stroke-width');
                    stroke_dasharray = node.attr('stroke-dasharray');
                }
                switch (drawType) {
                    case 'line':
                        line = me.lines[key];
                        data.lines.push({
                            id: key,
                            drawType: drawType,
                            fill: fill || '',
                            stroke: stroke || '',
                            strokeWidth: strokewidth || 1,
                            strokeDasharray: stroke_dasharray || '',
                            zindex: v.idx,
                            from: line.from || 'x',
                            to: line.to || 'x',
                            fdir: line.fdir || '',
                            tdir: line.tdir || '',
                            lineType: line.type || 1,
                            arrPos: line.arrPos || '',
                            arrType: line.arrType || 2,
                            x1: line.fCoor.x || 0,
                            y1: line.fCoor.y || 0,
                            x2: line.tCoor.x || 0,
                            y2: line.tCoor.y || 0
                        });
                        break;
                    case 'text':
                        bb = node.getBBox();
                        attrs = node[0].attr('transform');
                        data.texts.push({
                            id: key,
                            drawType: drawType,
                            pid: me.relTree[key] ? me.relTree[key].pid : undefined,
                            zindex: v.idx,
                            url: v.hLink || '',
                            txtStr: v.txt || '',
                            txtData: MT.JSON.stringify(v.txtData || {}),
                            x: bb.x,
                            y: bb.y-1,
                            rotate: null/*attrs[0] && attrs[0][1] ? attrs[0][1] : ''*/
                        });
                        break;
                    default:
                        bb = node.getBBox();
                        attrs = node.attr('transform');
                        data.shapes.push({
                            id: key,
                            drawType: drawType,
                            url: v.hLink || '',
                            fill: fill || '',
                            stroke: stroke || '',
                            strokeWidth: strokewidth || 1,
                            strokeDasharray: stroke_dasharray || '',
                            zindex: v.idx,
                            x: bb.x,
                            y: bb.y,
                            width: bb.width,
                            height: bb.height,
                            r: typeof node.attrs.r != 'undefined' ? node.attrs.r : '',
                            rotate: null/*attrs[0] && attrs[0][1] ? attrs[0][1] : ''*/
                        });
                }
            });
            if (!saveFlag) {
                me.ct.unmask();
                return false;
            }
            if (!me.saveTpl) {
                me.saveTpl = MT.Template(me.tpls.dataXML);
            }
            out = me.saveTpl.render(data);
            $.ajax({
                type: 'POST', url: me.urls.saveRData,
                data: {mapXml:escape(out), mapId:data.uuid, mapName: nowData.name},
                success: function(rs) {
                    me.ct.unmask();
                    if (rs.Success) {
                        me.tipMsg('保存成功！');
                    } else {
                        me.tipMsg(rs.msgError);
                    }
                },
                error: function(e) {
                    me.ct.unmask();
                    throw new Error(e.message);
                }
            });
        },
        loadData: function(uuid) {
            var me = this, paper = me.paper, url;
            me.editState = false;
            me.ct.mask('数据加载中，请稍后...');
            url = me.urls.getEditDt.replace("{id}", uuid);
            $.ajax({
                url: url, cache: false, type: 'POST',
                success: function(rs) {
                    if (rs) {
                        me.resetData();
                        rs = paper.loadXML(rs);
                        var canvs = rs.getElementsByTagName('canvas')[0],
                            setting = {}, nodes = {}, idx, idxs=[], count = 1, renderArg, pid,
                            node, nodeName;
                        MT.each(canvs.attributes || [], function(v, i) {
                            setting[v.name] = v.value;
                        });
                        me.paperCfg(setting);
                        node = canvs.nextSibling;
                        while (node) {
                            if (node.nodeType < 3) {
                                idx = node.getAttribute('idx') || (count++);
                                nodeName = node.nodeName.toLowerCase();
                                switch (nodeName) {
                                    case 'rshape':
                                        renderArg = paper.shapeArgs(me, node);
                                        break;
                                    case 'rtext':
                                        renderArg = paper.textArgs(me, node);
                                        pid = node.getAttribute('pid');
                                        break;
                                    case 'rline':
                                        renderArg = paper.lineArgs(me, node);
                                }
                                idxs.push(parseInt(idx, 10) || 0);
                                nodes['idx'+idx] = {nodeName: nodeName, args: renderArg, idx: idx, pid:pid};
                            }
                            node = node.nextSibling;
                        }
                        me.analysisNode(idxs, nodes);
                        idxs = [];
                        nodes = undefined;
                        delete nodes;
                    }
                    me.ct.unmask();
                },
                error: function(e) {
                    me.ct.unmask();
                    throw new Error(e.message);
                }
            });
        },
        analysisNode: function(idxs, nodes) {
            var me = this, i = 0, len, nodeObj, cid, pid, idx;
            idxs._quickSort1(); // quick sort
            len = idxs.length;
            for (; i < len; ++i) {
                nodeObj = nodes['idx'+idxs[i]];
                idx = parseInt(nodeObj.idx, 10) || 1;
                switch (nodeObj.nodeName) {
                    case 'rshape':
                        me.renderShape.apply(me, nodeObj.args || []);
                        break;
                    case 'rtext':
                        cid = me.doRenderText.apply(me, nodeObj.args || []);
                        pid = nodeObj.pid;
                        pid && me.addRelTree(pid, cid);
                        break;
                    case 'rline':
                        me.renderLine.apply(me, nodeObj.args || []);
                }
                if (me.index < idx) me.index = idx + 1;
            }
            nodes = undefined;
            delete nodes;
        },
        tipMsg: function(msg) {
            var me = this;
            if (!me.tipPne) {
                me.tipPne = new MT.ui.Panel({
                    noHeader: true,
                    width: 200, height: 30,
                    renderTo: document.body,
                    floating: true, bodyStyle: 'background-color:#68af02'
                });
            }
            try {
                me.tipPne.update('<div class="edtmsg">'+msg+'</div>');
                me.tipPne.node.alignTo(me.ct.childCt.body, 't-t').show();
                setTimeout(function() {
                    me.tipPne.hide();
                }, 2000);
            } catch (e) {
            }
        }
    };

    tpls = {
        exList: [
            '<div class="exlist"><ul>',
            '{#each data as mp,index}',
                '<li><input type="radio" id="edtlist_${index}" name="selGraph" value="${mp.id}"${index > 0 ? "":" checked"} /> ',
                '<label for="edtlist_${index}">${mp.name}</label>',
                '</li>',
            '{#/each}',
            '</ul></div>'
        ],
        paperProp: [
            '<table class="setpne">',
                '<tr><td>画布宽度：</td><td><input type="text" name="paperW" class="ed-num" value="${width}" /> 像素</td></tr>',
                '<tr><td>画布高度：</td><td><input type="text" name="paperH" class="ed-num" value="${height}" /> 像素</td></tr>',
                '<tr><td>背景颜色：</td><td><input type="text" class="colorSel" style="background-color:${bgColor}" readOnly name="colorSel" /></td></tr>',
                '<tr><td>背景图片：</td>',
                '<td colspan="2"><select name="selImgs" style="width:130px"><option value="">无背景图</option>',
                '{#each smp as mp,index}',
                    '<option value="${mp.id}"{#if selBgId==mp.id} selected{#/if}>${mp.name}</option>',
                '{#/each}',
                '</select></td></tr>',
            '</table>'
        ],
        txtM: [
            '<table style="margin:5px;">',
            '{#each txts as txt,index}',
            '<tr><td><input type="text" style="${txt.style}" class="txtinpt" value="${txt.text}" /></td><td>{#if index>0}<a href="javascript:void(0);" class="sm-removerow">—</a>{#/if}</td></tr>',
            '{#/each}',
            '</table>',
            '<a href="javascript:void(0);" class="sm-addrow">+添加行</a>'
        ],
        dataXML: [
        '<?xml version="1.0" encoding="UTF-8"?>',
        '<rGraphModel>',
            '<canvas bgImg="${bgImg}" bgColor="${bgColor}" width="${width}" height="${height}"/>',
            '{#each shapes as shape,idx}',
            '<rShape id="${shape.id}" type="${shape.drawType}" url="${shape.url}" idx="${shape.zindex}">',
                '<rCell{#if shape.fill} fill="${shape.fill}"{#/if}{#if shape.stroke} stroke="${shape.stroke}"{#/if}{#if shape.strokeWidth} stroke-width="${shape.strokeWidth}"{#/if}{#if shape.strokeDasharray} stroke-dasharray="${shape.strokeDasharray}"{#/if}>',
                    '<rGeometry x="${shape.x}" y="${shape.y}" width="${shape.width}" height="${shape.height}"{#if shape.r} r="${shape.r}"{#/if}{#if shape.rotate} rotate="${shape.rotate}"{#/if}/>',
                '</rCell>',
            '</rShape>',
            '{#/each}',
            '{#each texts as txt,idx1}',
            '<rText id="${txt.id}" type="${txt.drawType}" url="${txt.url}" pid="${txt.pid}" idx="${txt.zindex}">',
                '<rCell>',
                    '<rTxt>$*{txt.txtStr}</rTxt>',
                    '<rData>$*{txt.txtData}</rData>',
                    '<rGeometry x="${txt.x}" y="${txt.y}"{#if txt.rotate} rotate="${txt.rotate}"{#/if}/>',
                '</rCell>',
            '</rText>',
            '{#/each}',
            '{#each lines as line,idx2}',
            '<rLine id="${line.id}" type="${line.drawType}" idx="${line.zindex}">',
                '<rCell{#if line.fill} fill="${line.fill}"{#/if}{#if line.stroke} stroke="${line.stroke}"{#/if}{#if line.strokeWidth} stroke-width="${line.strokeWidth}"{#/if}{#if line.strokeDasharray} stroke-dasharray="${line.strokeDasharray}"{#/if}>',
                    '<rFt from="${line.from}" to="${line.to}" fdir="${line.fdir}" tdir="${line.tdir}" type="${line.lineType}" arrPos="${line.arrPos}" arrType="${line.arrType}"/>',
                    '<rGeometry x1="${line.x1}" y1="${line.y1}" x2="${line.x2}" y2="${line.y2}"/>',
                '</rCell>',
            '</rLine>',
            '{#/each}',
        '</rGraphModel>'
        ]
    };

    exports.init = function(cfg) {
        return new SubMap(cfg);
    };
});