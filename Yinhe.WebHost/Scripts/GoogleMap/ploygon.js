var creator = null;

var currentColor = "green";

var defaultColor = "white";


function PolygonCreator(map) {
    this.map = map;
    this.pen = new Pen(this.map, "red", this);
    this.penGreen = new Pen(this.map, "green", this);
    this.penYellow = new Pen(this.map, "yellow", this);
    this.penBlue = new Pen(this.map, "blue", this);
    this.penWhite = new Pen(this.map, "#CC0000", this);

    this.nowPen = null;

    this.editPolygonFlag = true;   //是否可以修改区域的标志（用于在情报浏览中查看地块红线）

    //设置完当前Pen的回调函数
    this.setNowPenHandle = null;
    this.selectPolygonHandle = null;
    this.areaCheckHandle = null;
    this.polyLineClickHandle = null;
    //this.initFinishHandle = null;   //在完全初始化完成之后调用

    //折线表
    this.PolyLineList = new Array();
    //自动增加笔列表
    this.penList = new Array();

    this.addPen = function (color, coorData, remark) {
        var newPen = null
        if (color) newPen = new Pen(this.map, color, this);
        else newPen = new Pen(this.map, defaultColor, this);
        this.penList.push(newPen);
        newPen.penIndex = this.penList.length - 1;
        if (remark) newPen.remark = remark;
        if (coorData) {
            this.setPath(newPen.penIndex, coorData);
        }
        this.setPenAdd(this.penList.length - 1);
        return this.penList.length - 1;
    }

    this.deletePen = function (index) {
        this.penList[index].polygon.remove();
        this.penList[index].polygon = null;
        this.penList[index].deleteFlag = true;
    }
    this.outputPaths2 = function () {
        var tmpPen = null;
        var ans = "";
        ans += '{"areaData":[';
        for (var penIndex = 0; penIndex < this.penList.length; penIndex++) {
            if (!this.penList[penIndex].deleteFlag) {
                tmpPen = this.penList[penIndex];

                if (tmpPen != null && tmpPen.polygon != null) {
                    var paths = tmpPen.polygon.getPolygonObj().getPaths().getArray();
                    if (ans.length > 15) ans += ',';
                    ans += '{"color":"' + tmpPen.color.toString() + '"';
                    ans += ',"remark":"' + tmpPen.remark + '",';
                    ans += '"coor":[';
                    for (var i = 0; i != paths.length; i++) {
                        if (i != 0) { ans += ','; }
                        ans += '['
                        var path = paths[i].getArray();
                        for (var j = 0; j != path.length; j++) {
                            if (j != 0) ans += ',';
                            ans += '{"Ya":"' + path[j].lat().toString() + '","Za":"' + path[j].lng().toString() + '"}';
                        }
                        ans += ']';
                    }
                    ans += ']}';
                }
            }
        }
        ans += ']}';
        return ans;
    }
    this.outputPaths = function () {
        var tmpPen = null;
        var ans = "";
        ans += '{"areaData":[';
        for (var penIndex = 0; penIndex < this.penList.length; penIndex++) {
            if (!this.penList[penIndex].deleteFlag) {
                tmpPen = this.penList[penIndex];

                if (tmpPen != null && tmpPen.polygon != null) {
                    var paths = tmpPen.polygon.getPolygonObj().getPaths().getArray();
                    if (ans.length > 15) ans += ',';
                    ans += '{"color":"' + tmpPen.color.toString() + '"';
                    ans += ',"remark":"' + tmpPen.remark + '",';
                    ans += '"coor":[';
                    for (var i = 0; i != paths.length; i++) {
                        if (i != 0) { ans += ','; }
                        ans += '['
                        var path = paths[i].getArray();
                        for (var j = 0; j != path.length; j++) {
                            if (j != 0) ans += ',';
                            ans += '{"Ya":"' + path[j].lat().toString() + '","Za":"' + path[j].lng().toString() + '"}';
                        }
                        ans += ']';
                    }
                    ans += ']}';
                }
            }
        }
        ans += ']}';
        return ans;
    }
    this.getPath = function (pen) {
        var tmpPen = null;
        var ans = "";
        ans += '{';

        tmpPen = pen;

        if (tmpPen != null && tmpPen.polygon != null) {
            var paths = tmpPen.polygon.getPolygonObj().getPaths().getArray();
            if (ans.length > 5) ans += ',';
            ans += '"coor":[';
            for (var i = 0; i != paths.length; i++) {
                if (i != 0) { ans += ','; }
                ans += '['
                var path = paths[i].getArray();
                for (var j = 0; j != path.length; j++) {
                    if (j != 0) ans += ',';
                    ans += '{"Ya":"' + path[j].lat() + '","Za":"' + path[j].lng() + '"}';
                }
                ans += ']';
            }
            ans += ']';
        }
        ans += '}';
        return ans;
    }
    this.outputPathsRed = function () {
        var tmpPen = null;
        var ans = "";
        ans += '{';
        tmpPen = this.pen;

        if (tmpPen != null && tmpPen.polygon != null) {
            var paths = tmpPen.polygon.getPolygonObj().getPaths().getArray();
            ans += '"color":"' + tmpPen.color.toString() + '",';

            ans += '"coor":[';
            for (var i = 0; i != paths.length; i++) {
                if (i != 0) { ans += ','; }
                ans += '['
                var path = paths[i].getArray();
                for (var j = 0; j != path.length; j++) {
                    if (j != 0) ans += ',';
                    ans += '{"Ya":"' + path[j].Ya + '","Za":"' + path[j].Za + '"}';
                }
                ans += ']';
            }
            ans += ']';
        }
        ans += '}';
        return ans;
    }

    this.checkArea = function () {
        var ans = new Array()
        for (var i = 0; i != this.penList.length; i++) {
            if (this.penList[i].limtArea) {
                if (this.penList[i].computeArea() > this.penList[i].limtArea) {
                    ans.push(this.penList[i]);
                }
            }
        }
        return ans;
    }

    this.setPath = function (index, coorData) {
        var src = JSON.parse(coorData);
        var coor = src.coor;
        if (this.penList[index].polygon != null) {
            this.penList[index].polygon.remove();
            this.penList[index].polygon = null;
        }
        for (var i = 0; i != coor.length; i++) {
            for (var j = 0; j != coor[i].length; j++) {
                if (i != 0) { this.penList[index].addFlag = true; }
                var latLng = new google.maps.LatLng(coor[i][j].Ya, coor[i][j].Za);
                this.penList[index].draw(latLng);
            }
            if (i == 0) {
                this.penList[index].drawPloygon(this.penList[index].listOfDots);
            }
            else {
                this.penList[index].addPolygon(this.penList[index].listOfDots);
                this.penList[index].addFlag = true;
            }
        }
        this.penList[index].addFlag = false;
    }

    var defaultPolylineOpt = { "map": this.map, "strokeColor": "red" };


    //coorData 应该是一个数组，里面包含了Ya,Za两种属性，而且只能是一条路径（polyLine只能是一条折线）
    this.addPolyLinePath = function (coorData) {

        var polyLine = new google.maps.Polyline(defaultPolylineOpt);
        var path = polyLine.getPath();
        for (var i = 0; i != coorData.length; i++) {
            var coor = new google.maps.LatLng(coorData[i].Ya, coorData[i].Za);
            path.push(coor);
        }
        //再次加入起始点使得折线闭合
        var coor = new google.maps.LatLng(coorData[0].Ya, coorData[0].Za);
        path.push(coor);
        polyLine.setPath(path);
        if (this.PolyLineList == null) this.PolyLineList = new Array();
        this.PolyLineList.push(polyLine);

        google.maps.event.addListener(polyLine, 'click', function (event) {
            if (thisOjb.polyLineClickHandle != null) {
                thisOjb.polyLineClickHandle(this, event);
            }

        });
        return polyLine;
    }
    //移除所有的polyLine
    this.clearPolyline = function () {

        if (this.PolyLineList != null) {
            for (var i = 0; i != this.PolyLineList.length; i++) {
                this.PolyLineList[i].setMap(null);
            }
            this.PolyLineList = null;
        }

    }
    //获取一个变量的类型
    var getType = function (object) {
        var _t;
        return ((_t = typeof (object)) == "object" ? object == null && "null" || Object.prototype.toString.call(object).slice(8, -1) : _t).toLowerCase();
    }
    //判断o是否为字符串
    function isString(o) {
        return getType(o) == "string";
    }

    this.setGreenPath = function () {
        var src = JSON.parse('{"color":"green","coor":[[{"Ya":"29.828915872235097","Za":"121.52259822068788"},{"Ya":"29.8284318851613","Za":"121.52345652757265"},{"Ya":"29.826644835659536","Za":"121.52246947465517"},{"Ya":"29.825751298924935","Za":"121.51993746934511"},{"Ya":"29.8254534515714","Za":"121.51740546403505"},{"Ya":"29.824671598045114","Za":"121.51671881852724"},{"Ya":"29.82277278544576","Za":"121.51646132646181"},{"Ya":"29.821506890338934","Za":"121.51689047990419"},{"Ya":"29.8261236068686","Za":"121.51081795869447"},{"Ya":"29.827007832675932","Za":"121.51020641503908"},{"Ya":"29.827538364405044","Za":"121.51045317826845"},{"Ya":"29.827575594596052","Za":"121.51234145341493"},{"Ya":"29.82705437065961","Za":"121.51590342698671"},{"Ya":"29.827091601030943","Za":"121.51937956987001"},{"Ya":"29.828059585817115","Za":"121.52148242173769"}],[{"Ya":"29.824932216566346","Za":"121.51786680398561"},{"Ya":"29.824988063304037","Za":"121.51860709367372"},{"Ya":"29.824671598044656","Za":"121.51921863732912"},{"Ya":"29.824150358962438","Za":"121.51893968759157"},{"Ya":"29.82413174323067","Za":"121.51859636483766"},{"Ya":"29.82405262633192","Za":"121.51808674512483"},{"Ya":"29.824113127495416","Za":"121.51768977819063"},{"Ya":"29.824215513996364","Za":"121.51691193757631"},{"Ya":"29.8246343667718","Za":"121.51730890451051"}],[{"Ya":"29.826407490744213","Za":"121.51348407445528"},{"Ya":"29.82656426643204","Za":"121.5134481999097"},{"Ya":"29.82669777280715","Za":"121.51341232536413"},{"Ya":"29.826805683058456","Za":"121.51337913302757"},{"Ya":"29.826913593193225","Za":"121.5132708388386"},{"Ya":"29.82704331789106","Za":"121.5130649792967"},{"Ya":"29.82710788931463","Za":"121.51285911975481"},{"Ya":"29.827072404123033","Za":"121.51269215224363"},{"Ya":"29.826990380927054","Za":"121.51257882891275"},{"Ya":"29.82686647341824","Za":"121.51244404790975"},{"Ya":"29.82672395050683","Za":"121.51236291108705"},{"Ya":"29.82652267290615","Za":"121.51232670126535"},{"Ya":"29.826284164241823","Za":"121.51236559329607"},{"Ya":"29.826085794406932","Za":"121.51237699268438"},{"Ya":"29.82591534728411","Za":"121.51249568043329"},{"Ya":"29.825712322857513","Za":"121.51265191910841"},{"Ya":"29.825616336953452","Za":"121.51285107312776"},{"Ya":"29.825554091499754","Za":"121.51306095598318"},{"Ya":"29.82555700016665","Za":"121.51326011000253"},{"Ya":"29.82565065919637","Za":"121.51336002228834"},{"Ya":"29.8257163949847","Za":"121.51344920573808"},{"Ya":"29.825850193358367","Za":"121.51348541555978"},{"Ya":"29.826002606939646","Za":"121.51349480329134"},{"Ya":"29.82621202980196","Za":"121.51349480329134"}]]}');

        var coor = src.coor;
        for (var i = 0; i != coor.length; i++) {

            for (var j = 0; j != coor[i].length; j++) {
                if (i != 0) { this.penGreen.addFlag = true; }
                var latLng = new google.maps.LatLng(coor[i][j].Ya, coor[i][j].Za);
                this.penGreen.draw(latLng);
            }
            if (i == 0) {
                this.penGreen.drawPloygon(this.penGreen.listOfDots);
            }
            else {
                this.penGreen.addPolygon(this.penGreen.listOfDots);
                this.penGreen.addFlag = true;
            }
        }
    }
    this.setRedPathData = function (coorData) {
        var src = JSON.parse(coorData);
        var coor = src.coor;
        for (var i = 0; i != coor.length; i++) {

            for (var j = 0; j != coor[i].length; j++) {
                if (i != 0) { this.pen.addFlag = true; }
                var latLng = new google.maps.LatLng(coor[i][j].Ya, coor[i][j].Za);
                this.pen.draw(latLng);
            }
            if (i == 0) {
                this.pen.drawPloygon(this.pen.listOfDots);
            }
            else {
                this.pen.addPolygon(this.pen.listOfDots);
                this.pen.addFlag = true;
            }
        }
    }
    this.setRedPath = function () {
        var src = JSON.parse('{"color":"red","coor":[[{"Ya":"29.822381848962205","Za":"121.5212142008362"},{"Ya":"29.82229342264074","Za":"121.52424509702303"},{"Ya":"29.82204675933061","Za":"121.52423436818697"},{"Ya":"29.822000219015155","Za":"121.52108545480348"}],[{"Ya":"29.82645868283213","Za":"121.51119346795656"},{"Ya":"29.827149773458533","Za":"121.5109668212948"},{"Ya":"29.827412130685556","Za":"121.51182412235119"},{"Ya":"29.827022957523194","Za":"121.51433366416074"},{"Ya":"29.826412144571083","Za":"121.51765759168245"},{"Ya":"29.825890914566525","Za":"121.51678185043909"},{"Ya":"29.825220757708994","Za":"121.51616360126116"},{"Ya":"29.824196898277318","Za":"121.51598523436166"},{"Ya":"29.823061332822142","Za":"121.5160643595276"},{"Ya":"29.82457851983712","Za":"121.51396150765993"}],[{"Ya":"29.824057280269525","Za":"121.51687975106813"},{"Ya":"29.823978163311846","Za":"121.51800627885439"},{"Ya":"29.824141051097637","Za":"121.51913280664064"},{"Ya":"29.822940329185247","Za":"121.51950026927568"},{"Ya":"29.823438304752287","Za":"121.51998910186865"},{"Ya":"29.82319862523296","Za":"121.52088797716476"},{"Ya":"29.82276347745226","Za":"121.52070324001886"},{"Ya":"29.822116569763292","Za":"121.52074146149732"},{"Ya":"29.822018835144036","Za":"121.51966388402559"},{"Ya":"29.822130531843907","Za":"121.5188002127228"},{"Ya":"29.82157204709629","Za":"121.51713724313356"},{"Ya":"29.82208166455307","Za":"121.51694948850252"},{"Ya":"29.822777439442525","Za":"121.51680464921571"},{"Ya":"29.823417361904557","Za":"121.51677782712557"}]]}');

        var coor = src.coor;
        for (var i = 0; i != coor.length; i++) {

            for (var j = 0; j != coor[i].length; j++) {
                if (i != 0) { this.pen.addFlag = true; }
                var latLng = new google.maps.LatLng(coor[i][j].Ya, coor[i][j].Za);
                this.pen.draw(latLng);
            }
            if (i == 0) {
                this.pen.drawPloygon(this.pen.listOfDots);
            }
            else {
                this.pen.addPolygon(this.pen.listOfDots);
                this.pen.addFlag = true;
            }
        }
    }

    this.setYellowPath = function () {
        var src = JSON.parse('{"color":"yellow","coor":[[{"Ya":"29.828301580548708","Za":"121.52354235826112"},{"Ya":"29.82807820081736","Za":"121.52440066514589"},{"Ya":"29.824839138601476","Za":"121.52304883180238"},{"Ya":"29.82584437604098","Za":"121.52141804872133"},{"Ya":"29.826551759289217","Za":"121.52279133973695"}],[{"Ya":"29.825872299158892","Za":"121.520999624115"},{"Ya":"29.825504644148563","Za":"121.52156825242616"},{"Ya":"29.825234719355937","Za":"121.52199204145052"},{"Ya":"29.82482052299797","Za":"121.52173991380312"},{"Ya":"29.823610501333558","Za":"121.5210693615494"},{"Ya":"29.82391300812304","Za":"121.5202432411728"},{"Ya":"29.824262053280865","Za":"121.52000184236147"},{"Ya":"29.824741406644446","Za":"121.51933129010774"},{"Ya":"29.825155603330337","Za":"121.5186929243622"}],[{"Ya":"29.82465298241052","Za":"121.5221690672455"},{"Ya":"29.82407589601506","Za":"121.52324195085146"},{"Ya":"29.82234461683635","Za":"121.52281279740907"},{"Ya":"29.822437697124887","Za":"121.5212678450165"},{"Ya":"29.823256799926273","Za":"121.52137513337709"},{"Ya":"29.823089256717534","Za":"121.52246947465517"},{"Ya":"29.823536037983445","Za":"121.52259822068788"},{"Ya":"29.82388973840252","Za":"121.52180428681947"}],[{"Ya":"29.82387112262218","Za":"121.52392859635927"},{"Ya":"29.823349879365317","Za":"121.523520900589"},{"Ya":"29.823238184028046","Za":"121.52405734239198"},{"Ya":"29.822921713227938","Za":"121.524239732605"},{"Ya":"29.8226610894637","Za":"121.52399296937563"},{"Ya":"29.822791401430777","Za":"121.52326340852358"},{"Ya":"29.822400465019868","Za":"121.52311320481874"},{"Ya":"29.822377194947226","Za":"121.52425582585909"},{"Ya":"29.82291240524805","Za":"121.5242772835312"},{"Ya":"29.82368961858227","Za":"121.52429874120332"}],[{"Ya":"29.824410978843904","Za":"121.52349944291689"},{"Ya":"29.824950832149597","Za":"121.52405734239198"},{"Ya":"29.825043910011036","Za":"121.52461524186708"},{"Ya":"29.823684964628203","Za":"121.52465815721132"}],[{"Ya":"29.827538364405143","Za":"121.52459378419496"},{"Ya":"29.825807145205005","Za":"121.5245294111786"},{"Ya":"29.824969447728826","Za":"121.52360673127748"}]]}');

        var coor = src.coor;
        for (var i = 0; i != coor.length; i++) {

            for (var j = 0; j != coor[i].length; j++) {
                if (i != 0) { this.penYellow.addFlag = true; }
                var latLng = new google.maps.LatLng(coor[i][j].Ya, coor[i][j].Za);
                this.penYellow.draw(latLng);
            }
            if (i == 0) {
                this.penYellow.drawPloygon(this.penYellow.listOfDots);
            }
            else {
                this.penYellow.addPolygon(this.penYellow.listOfDots);
                this.penYellow.addFlag = true;
            }
        }
    }
    this.setBluePath = function () {
        var src = JSON.parse('{"color":"blue","coor":[[{"Ya":"29.825988645402727","Za":"121.52115787444689"},{"Ya":"29.82370823439979","Za":"121.52458841977693"},{"Ya":"29.82361050133692","Za":"121.52455891547777"},{"Ya":"29.825930472299408","Za":"121.52106667934038"}],[{"Ya":"29.825192834412565","Za":"121.52208591876604"},{"Ya":"29.825036929177998","Za":"121.52194107947923"},{"Ya":"29.82408054995447","Za":"121.52148242173769"},{"Ya":"29.822686686474192","Za":"121.52081991611101"},{"Ya":"29.822228266357367","Za":"121.52086014924623"},{"Ya":"29.822242228422436","Za":"121.52109886584856"},{"Ya":"29.822633165452206","Za":"121.52101303516008"},{"Ya":"29.82316837438244","Za":"121.52120481310465"},{"Ya":"29.823549999869336","Za":"121.52138318000414"},{"Ya":"29.824490095462536","Za":"121.52182306228258"},{"Ya":"29.82496828425877","Za":"121.52205373225786"},{"Ya":"29.825120699185018","Za":"121.52217711387254"}],[{"Ya":"29.823666348808093","Za":"121.52028079209902"},{"Ya":"29.823454593629002","Za":"121.52106667934038"},{"Ya":"29.823245164988663","Za":"121.52094597993471"},{"Ya":"29.823475536468894","Za":"121.52028347430803"},{"Ya":"29.823636098095687","Za":"121.51989924786665"},{"Ya":"29.823331263482793","Za":"121.51957939444162"},{"Ya":"29.82392173426518","Za":"121.51931067062594"},{"Ya":"29.82422860317108","Za":"121.51919910749473"},{"Ya":"29.824535471134798","Za":"121.51925652353145"},{"Ya":"29.82458288287837","Za":"121.51935551380791"},{"Ya":"29.824560485922515","Za":"121.51947596175648"},{"Ya":"29.82417130165544","Za":"121.51979732392408"},{"Ya":"29.823839708483355","Za":"121.51982984570839"}],[{"Ya":"29.824839138599796","Za":"121.52316684899904"},{"Ya":"29.827945568863953","Za":"121.52447308478929"},{"Ya":"29.827554652613763","Za":"121.52451600013353"},{"Ya":"29.82486473504896","Za":"121.52346189199068"},{"Ya":"29.824700684965844","Za":"121.52338679013826"}],[{"Ya":"29.82367798370036","Za":"121.52158702788927"},{"Ya":"29.823470882510193","Za":"121.52234609304048"},{"Ya":"29.823242838006617","Za":"121.52224416909792"},{"Ya":"29.82338245715221","Za":"121.52151192603685"}],[{"Ya":"29.823193971259577","Za":"121.5235369938431"},{"Ya":"29.82322422210582","Za":"121.52364428220369"},{"Ya":"29.823212587166047","Za":"121.52376229940035"},{"Ya":"29.823198625236543","Za":"121.52386690555193"},{"Ya":"29.82316604739342","Za":"121.52397151170351"},{"Ya":"29.82311718060888","Za":"121.5240425902424"},{"Ya":"29.823065986808906","Za":"121.52408416448213"},{"Ya":"29.822993850046295","Za":"121.52412037430383"},{"Ya":"29.822924040226447","Za":"121.5241324442444"},{"Ya":"29.82286237484503","Za":"121.52411769209482"},{"Ya":"29.82279605543011","Za":"121.52407075343706"},{"Ya":"29.822782093442417","Za":"121.52379716811754"},{"Ya":"29.82289146229386","Za":"121.52360404906847"},{"Ya":"29.822993559172204","Za":"121.523520900589"},{"Ya":"29.823025846197467","Za":"121.52340020118334"},{"Ya":"29.82305231573291","Za":"121.52330632386781"},{"Ya":"29.823120671094262","Za":"121.52324463306047"},{"Ya":"29.82313259691851","Za":"121.5233398514805"},{"Ya":"29.823107290899532","Za":"121.52344043431856"}]]}');

        var coor = src.coor;
        for (var i = 0; i != coor.length; i++) {

            for (var j = 0; j != coor[i].length; j++) {
                if (i != 0) { this.penBlue.addFlag = true; }
                var latLng = new google.maps.LatLng(coor[i][j].Ya, coor[i][j].Za);
                this.penBlue.draw(latLng);
            }
            if (i == 0) {
                this.penBlue.drawPloygon(this.penBlue.listOfDots);
            }
            else {
                this.penBlue.addPolygon(this.penBlue.listOfDots);
                this.penBlue.addFlag = true;
            }
        }
    }
    this.setWhitePath = function () {
        var src = JSON.parse('{"color":"#CC0000","coor":[[{"Ya":"29.829157864892995","Za":"121.52266259370424"},{"Ya":"29.828189890745396","Za":"121.52493710694887"},{"Ya":"29.823526730060756","Za":"121.52478690324403"},{"Ya":"29.82332195554275","Za":"121.5246903437195"},{"Ya":"29.823470882506555","Za":"121.52442212281801"},{"Ya":"29.822018835143957","Za":"121.52440066514589"},{"Ya":"29.82192575446536","Za":"121.52440066514589"},{"Ya":"29.821814057536642","Za":"121.51977653680422"},{"Ya":"29.821357960448967","Za":"121.51699776826479"},{"Ya":"29.826896141426715","Za":"121.50990600762941"},{"Ya":"29.828050278315736","Za":"121.5103351610718"},{"Ya":"29.828050278315736","Za":"121.51196594415285"},{"Ya":"29.82732429053752","Za":"121.5193044680176"},{"Ya":"29.828664571547062","Za":"121.52123565850832"}]]}');

        var coor = src.coor;
        for (var i = 0; i != coor.length; i++) {

            for (var j = 0; j != coor[i].length; j++) {
                if (i != 0) { this.penWhite.addFlag = true; }
                var latLng = new google.maps.LatLng(coor[i][j].Ya, coor[i][j].Za);
                this.penWhite.draw(latLng);
            }
            if (i == 0) {
                this.penWhite.drawPloygon(this.penWhite.listOfDots);
            }
            else {
                this.penWhite.addPolygon(this.penWhite.listOfDots);
                this.penWhite.addFlag = true;
            }
        }
    }

    this.inputPaths = function (data) {
        this.setGreenPath();
        this.setYellowPath();
        this.setBluePath();
        this.setRedPath();
        this.setEditdisable();
    }

    var thisOjb = this;

    this.drawingManager = new google.maps.drawing.DrawingManager({
        drawingMode: google.maps.drawing.OverlayType.POLYGON,
        drawingControl: true,
        drawingControlOptions: {
            position: google.maps.ControlPosition.TOP_CENTER,
            drawingModes: [google.maps.drawing.OverlayType.CIRCLE, google.maps.drawing.OverlayType.RECTANGLE, google.maps.drawing.OverlayType.POLYGON]
        },
        circleOptions: {
            editable: true
        },
        rectangleOptions: {
            editable: true
        },
        polygonOptions: {
            editable: true
        }
    });

    creator = this;

    this.PolygonResizeHandle = null;

    //描点时的点击事件
    this.event = google.maps.event.addListener(thisOjb.map, 'click', function (event) {
        if (thisOjb.nowPen != null) {
            thisOjb.nowPen.draw(event.latLng);
        }
        else {
            thisOjb.setEditdisable();
        }
    });

    this.showData = function (penIndex) {
        if (penIndex) return this.pen.getData();
        else {
            return this.penList[penIndex].getData();
        }
    }

    this.showColor = function (penIndex) {
        if (penIndex) return this.pen.getColor();
        else {
            return this.penList[penIndex].getColor();
        }
    }
    this.destroy = function () {
        this.pen.deleteMis(); if (null != this.pen.polygon) { this.pen.polygon.remove(); }
        this.penGreen.deleteMis(); if (null != this.penGreen.polygon) { this.penGreen.polygon.remove(); }
        this.penYellow.deleteMis(); if (null != this.penYellow.polygon) { this.penYellow.polygon.remove(); }
        this.penBlue.deleteMis(); if (null != this.penBlue.polygon) { this.penBlue.polygon.remove(); }
        this.penWhite.deleteMis(); if (null != this.penWhite.polygon) { this.penWhite.polygon.remove(); }
        if (thisOjb.penList != null) {
            for (var i = 0; i != thisOjb.penList.length; i++) {
                thisOjb.penList[i].deleteMis(); if (null != this.penList[i].polygon) { this.penList[i].polygon.remove(); }
            }
        }
        if (this.PolyLineList != null) {
            for (var i = 0; i != thisOjb.PolyLineList.length; i++) {
                thisOjb.PolyLineList[i].setMap(null);
            }
            this.PolyLineList = null;
        }
        google.maps.event.removeListener(this.event);
    }

    this.changePenRed = function () {
        if (thisOjb.nowPen != thisOjb.pen) {
            thisOjb.nowPen = thisOjb.pen;
        }
        if (this.setNowPenHandle != null) {
            this.setNowPenHandle(this.nowPen);
        }
    }
    this.changePenGreen = function () {
        if (thisOjb.nowPen != thisOjb.penGreen) {
            thisOjb.nowPen = thisOjb.penGreen;
        }
        if (this.setNowPenHandle != null) {
            this.setNowPenHandle(this.nowPen);
        }
    }
    this.changePenYellow = function () {
        if (thisOjb.nowPen != thisOjb.penYellow) {
            thisOjb.nowPen = thisOjb.penYellow;
        }
        if (this.setNowPenHandle != null) {
            this.setNowPenHandle(this.nowPen);
        }
    }
    this.changePenBlue = function () {
        if (thisOjb.nowPen != thisOjb.penBlue) {
            thisOjb.nowPen = thisOjb.penBlue;
        }
        if (this.setNowPenHandle != null) {
            this.setNowPenHandle(this.nowPen);
        }
    }
    this.changePenWhite = function () {
        if (thisOjb.nowPen != thisOjb.penWhite) {
            thisOjb.nowPen = thisOjb.penWhite;
        }
        if (this.setNowPenHandle != null) {
            this.setNowPenHandle(this.nowPen);
        }
    }

    //根据序号选择笔
    this.changePen = function (index) {
        //        alert("changePen:" + index);
        if (index >= this.penList.length || index < 0) {
            alert("changePen ---> 非法的index");
            return;
        }
        if (thisOjb.nowPen != thisOjb.penList[index]) {

            thisOjb.nowPen = thisOjb.penList[index];
        }
        //        alert("this.setNowPenHandle:" + this.setNowPenHandle)
        if (this.setNowPenHandle != null) {
            this.setNowPenHandle(this.nowPen);
        }
    }

    this.resetNowPen = function () {
        try {
            if (this.nowPen.polygon == null) this.nowPen.deleteMis();
        } catch (e) { }
        var color = this.nowPen.getColor();
        this.setEditdisable();
        this.nowPen.deleteMis(); if (null != this.penWhite.polygon) { this.penWhite.polygon.remove(); }
        this.nowPen = new Pen(this.map, color, this);

    }
    this.resetPenRed = function () {
        try {
            if (this.nowPen.polygon == null) this.nowPen.deleteMis();
        } catch (e) { }
        this.setEditdisable();
        this.pen.deleteMis(); if (null != this.pen.polygon) { this.pen.polygon.remove(); }
        this.pen = new Pen(this.map, "red", this);
        this.nowPen = this.pen;
    }
    this.resetPenGreen = function () {
        try {
            if (this.nowPen.polygon == null) this.nowPen.deleteMis();
        } catch (e) { }
        this.setEditdisable();
        this.penGreen.deleteMis(); if (null != this.penGreen.polygon) { this.penGreen.polygon.remove(); }
        this.penGreen = new Pen(this.map, "green", this);
        this.nowPen = this.penGreen;
    }
    this.resetPenYellow = function () {
        try {
            if (this.nowPen.polygon == null) this.nowPen.deleteMis();
        } catch (e) { }
        this.setEditdisable();
        this.penYellow.deleteMis(); if (null != this.penYellow.polygon) { this.penYellow.polygon.remove(); }
        this.penYellow = new Pen(this.map, "yellow", this);
        this.nowPen = this.penYellow;
    }
    this.resetPenBlue = function () {
        try {
            if (this.nowPen.polygon == null) this.nowPen.deleteMis();
        } catch (e) { }
        this.setEditdisable();
        this.penBlue.deleteMis(); if (null != this.penBlue.polygon) { this.penBlue.polygon.remove(); }
        this.penBlue = new Pen(this.map, "blue", this);
        this.nowPen = this.penBlue;
    }
    this.resetPenWhite = function () {
        try {
            if (this.nowPen.polygon == null) this.nowPen.deleteMis();
        } catch (e) { }
        this.setEditdisable();
        this.penWhite.deleteMis(); if (null != this.penWhite.polygon) { this.penWhite.polygon.remove(); }
        this.penWhite = new Pen(this.map, "#CC0000", this);
        this.nowPen = this.penWhite;
    }

    this.resetPenBlue = function (penIndex) {
        try {
            if (this.penList[penIndex].polygon == null) this.penList[penIndex].deleteMis();
        } catch (e) { }
        this.setEditdisable();
        this.penList[penIndex].deleteMis(); if (null != this.penList[penIndex].polygon) { this.penList[penIndex].polygon.remove(); }
        this.penList[penIndex] = new Pen(this.map, defaultColor, this);
        this.nowPen = this.penList[penIndex];
    }

    this.setEditdisable = function () {
        if (this.pen != null) { this.pen.setEdit(false); }
        if (this.penBlue != null) { this.penBlue.setEdit(false); }
        if (this.penYellow != null) { this.penYellow.setEdit(false); }
        if (this.penGreen != null) { this.penGreen.setEdit(false); }
        if (this.penWhite != null) { this.penWhite.setEdit(false); }

        for (var i = 0; i != thisOjb.penList.length; i++) {
            if (thisOjb.penList[i] != null) {
                thisOjb.penList[i].setEdit(false);
            }
        }
    }
    this.setPenGreenAdd = function () {
        try {
            if (this.nowPen.polygon == null) this.nowPen.deleteMis();
        } catch (e) { }
        this.setEditdisable();
        this.nowPen = this.penGreen;
        this.penGreen.setAdd(true);
    }
    this.setPenRedAdd = function () {
        try {
            if (this.nowPen.polygon == null) this.nowPen.deleteMis();
        } catch (e) { }
        this.setEditdisable();
        this.nowPen = this.pen;
        this.pen.setAdd(true);
    }
    this.setPenBlueAdd = function () {
        try {
            if (this.nowPen.polygon == null) this.nowPen.deleteMis();
        } catch (e) { }
        this.setEditdisable();
        this.nowPen = this.penBlue;
        this.penBlue.setAdd(true);
    }
    this.setPenYellowAdd = function () {
        try {
            if (this.nowPen.polygon == null) this.nowPen.deleteMis();
        } catch (e) { }
        this.setEditdisable();
        this.nowPen = this.penYellow;
        this.penYellow.setAdd(true);
    }
    this.setPenWhiteAdd = function () {
        try {
            if (this.nowPen.polygon == null) this.nowPen.deleteMis();
        } catch (e) { }
        this.setEditdisable();
        this.nowPen = this.penWhite;
        this.penWhite.setAdd(true);
    }

    this.setPenAdd = function (penIndex) {
        flag = "false";
        try {
            if (this.nowPen.polygon == null) this.nowPen.deleteMis();
        } catch (e) { }
        this.setEditdisable();
        this.nowPen = this.penList[penIndex];
        for (var i = 0; i != this.penList.length; i++) {
            this.penList[penIndex].setAdd(false);
        }
        this.penList[penIndex].setAdd(true);
    }

    this.setPenColor = function (color, penIndex) {
        if (!penIndex) {
            if (this.pen != null) {
                this.pen.setColor(color);
            }
        }
        else {
            this.penList[penIndex].setColor(color);
        }

    }
}

function Pen(map, color, creator, remark, fillOpacity) {
    var penObj = this;
    this.setAdd = function (para) {
        if (para == null) { this.addFlag = false; return; }
        this.addFlag = para;
    }
    this.limtArea = 0;
    this.color = color;
    this.map = map;
    //用于pen的标注（区域标示）
    this.remark = remark;
    this.listOfDots = new Array();
    this.polyline = null;
    this.polygon = null;
    this.currentDot = null;
    this.deleteFlag = false;

    this.creator = creator;
    this.placeMark = null;

    this.polygonOpt = { "fillOpacity": "0.2", "map": this.map, "fillColor": this.color };
    if (fillOpacity) { this.polygonOpt.fillOpacity = fillOpacity; }
    if (this.color == '#FF0000') { this.polygonOpt.fillOpacity = 0; }

    this.addFlag = false;

    this.PolygonChangeHandle = function () {
        var para = new Object();
        para.area = this.computeArea();
        para.length = this.computeLength();
        para.color = this.color;

        if (creator.PolygonResizeHandle != null) {
            creator.PolygonResizeHandle(para);
        }
        if (creator.selectPolygonHandle != null) {
            creator.selectPolygonHandle(penObj);
        }
    }

    this.computeArea = function () {
        if (this.polygon == null) return 0;
        if (this.polygon.polygonObj == null) return 0;
        var paths = this.polygon.polygonObj.getPaths().getArray();
        var area = 0;
        for (var i = 0; i != paths.length; i++) {
            area += google.maps.geometry.spherical.computeArea(paths[i]);
        }
        return area;
    }
    this.computeLength = function () {
        var paths = this.polygon.polygonObj.getPaths().getArray();
        var length = 0;
        for (var i = 0; i != paths.length; i++) {
            length += google.maps.geometry.spherical.computeLength(paths[i]);
        }
        return length;
    }

    this.draw = function (latLng) {
        if (null != this.polygon && !this.addFlag) { this.creator.setEditdisable(); }
        else {
            if (this.addFlag) {
                if (this.currentDot != null && this.listOfDots.length > 1 &&
                this.currentDot == this.listOfDots[0]) {
                    //将这些点丢到新的区域内
                    this.addPolygon(this.listOfDots);
                    flag = "true";
                }
                else {
                    if (null != this.polyline) { this.polyline.remove(); }
                    var dot = new Dot(latLng, this.map, this); this.listOfDots.push(dot);
                    if (this.listOfDots.length > 1) { this.polyline = new Line(this.listOfDots, this.map, this.color); }
                }
            }
            else {
                if (this.currentDot != null && this.listOfDots.length > 1 &&
                this.currentDot == this.listOfDots[0]) {
                    //将这些点绘制为Polygon
                    this.drawPloygon(this.listOfDots);
                    flag = "true";
                }
                else {
                    if (null != this.polyline) { this.polyline.remove(); }
                    var dot = new Dot(latLng, this.map, this); this.listOfDots.push(dot);
                    if (this.listOfDots.length > 1) { this.polyline = new Line(this.listOfDots, this.map, this.color); }
                }
            }
        }
        if (creator.selectPolygonHandle != null) {
            try {
                creator.selectPolygonHandle(this);
            } catch (e) { }
        }
    }

    this.setEdit = function (val) { if (this.polygon != null) { this.polygon.setEditable(val); } }     //修改可编辑性

    this.fillOpc = 0.1;    //透明度参数

    this.drawPloygon = function (listOfDots, color, des, id) {

        this.polygon = new Polygon(listOfDots, this.map, this, this.color, des, id, this.fillOpc);
        this.polygon.getPolygonObj().setOptions(this.polygonOpt);
        this.deleteMis();
        this.setEdit(true);
        var _this = this;
        //多边形区域的点击事件
        google.maps.event.addListener(this.polygon.getPolygonObj(), 'click', function (event) {



            if (yinhoo.googleMap_LastDot != null) {
                yinhoo.googleMap_LastDot.setMap(null);
            }
            yinhoo.googleMap_LastDot = new google.maps.Marker({
                position: event.latLng,
                map: map,
                draggable: true,
                animation: google.maps.Animation.DROP,
                shape: { type: 'circle' },
                zIndex: 9999,
                icon: "/Content/images/icon/GoogleMapIcon/marker_gold.png"
            });
            //设置保存用的坐标位置
            $("#lat").attr("value", event.latLng.lat().toString());
            $("#lng").attr("value", event.latLng.lng().toString());
            lat = event.latLng.lat().toString();
            lng = event.latLng.lng().toString();
            yinhoo.googleMap_LastDot.defaultIcon = "Content/images/icon/GoogleMapIcon/marker_gold.png";
            if (yinhoo.googleMap_ClickEvent != null) {
                yinhoo.googleMap_ClickEvent(event);
            }





            box("/DesignManage/projInfo?engId=" + engId + "&r=" + Math.random(), { boxid: "aa", title: '新增节点', contentType: 'ajax', width: 480,
                onOpen: function () {

                },
                submit_cb: function () {
                    var projId = $("input[name=projName]:checked").val();
                    var tbName = "XH_DesignManage_Project";
                    var queryStr = "db.XH_DesignManage_Project.distinct('_id',{'projId':'" + projId + "'})";

                    if (lng != "" && projId != "") {
                        $.ajax({
                            url: "/Home/SavePostInfo",
                            type: 'post',
                            data: {
                                tbName: tbName,
                                queryStr: queryStr,
                                isInitPath: 1,
                                lng: lng,
                                lat: lat
                            },
                            dataType: 'json',
                            error: function () {
                                $.tmsg("m_jfw", "未知错误，请联系服务器管理员，或者刷新页面重试", { infotype: 2 });
                            },
                            success: function (data) {
                                if (data.Success == false) {
                                    alert(data.Message);
                                }
                                else {

                                    window.location.reload();
                                }
                            }
                        });
                    }
                }
            });





            //            for (var i = 0; i != creator.penList.length; i++) {
            //设置图像zIndex 的方法有问题
            //                creator.penList[i].polygon.polygonObj.setOptions('{"zIndex": 0}');
            //            }
            //            var zIndex = penObj.creator.penList.length + 5;

            if (!creator.editPolygonFlag) { return; }   //若是不能修改则事件结束

            creator.setEditdisable();
            penObj.polygon.setEditable(true);
            //            alert(creator.selectPolygonHandle);
            if (creator.selectPolygonHandle != null) {
                creator.selectPolygonHandle(penObj);
            }
        });

        //多边形区域的按下事件
        google.maps.event.addListener(this.polygon.getPolygonObj(), 'rightclick', function (event) {
            if (!creator.editPolygonFlag) { return; }   //若是不能修改则事件结束
            penObj.polygon.remove();
            penObj.polygon = null;

        });
        //计算面积与周长并返回给页面
        try {
            this.PolygonChangeHandle();
        } catch (e) {

        }
    }

    this.addPolygon = function (listOfDots) {
        //        var array = new google.maps.MVCArray<google.maps.LatLng>;
        //        for (var i = 0; i != listOfDots.length; i++) {
        //            var latLng = new google.maps.LatLng(listOfDots[i].Ya, listOfDots[i].Za);
        //            array.insertAt(array.getLength(), latLng);
        //        }
        var tmpPolygon = new Polygon(listOfDots, null, this, null, null);
        var path = tmpPolygon.getPolygonObj().getPath();

        if (this.polygon == null) {
            this.drawPloygon(listOfDots);
            this.addFlag = false;
        }
        else {
            this.polygon.getPolygonObj().getPaths().push(path);
            this.polygon.getPolygonObj().setPaths(this.polygon.getPolygonObj().getPaths());
            this.deleteMis();
            this.addFlag = false;
        }
        //计算面积与周长并返回给页面
        try {
            this.PolygonChangeHandle();
        } catch (e) {
            //            alert(e.Message);
        }
    }

    this.deleteMis = function () { $.each(this.listOfDots, function (index, value) { value.remove(); }); this.listOfDots.length = 0; if (null != this.polyline) { this.polyline.remove(); this.polyline = null; } }

    this.cancel = function () {
        if (null != this.polygon) { (this.polygon.remove()); }
        this.polygon = null; this.deleteMis();
    }
    this.setCurrentDot = function (dot) { this.currentDot = dot; }
    this.getListOfDots = function () { return this.listOfDots; }
    this.getData = function () { if (this.polygon != null) { var data = ""; var paths = this.polygon.getPlots(); paths.getAt(0).forEach(function (value, index) { data += (value.toString()); }); return data; } else { return null; } }
    this.getColor = function () { if (this.polygon != null) { var color = this.polygon.getColor(); return color; } else { return this.color; } }

    this.setColor = function (color) {
        this.color = color;
        this.polygonOpt = { "fillOpacity": "0.2", "map": this.map, "fillColor": this.color };
        if (this.polygon != null) {
            this.polygon.setColor(color);

        }
    }
}

function Dot(latLng, map, pen) {
    this.latLng = latLng;
    this.parent = pen;
    this.markerObj = new google.maps.Marker({ position: this.latLng, map: map });
    this.markerObj.setTitle('titel');
    this.addListener = function () {
        var parent = this.parent;
        var thisMarker = this.markerObj;
        var thisDot = this;
        google.maps.event.addListener(thisMarker, 'click', function () {
            parent.setCurrentDot(thisDot);
            parent.draw(thisMarker.getPosition());
        });
    }
    this.addListener();
    this.getLatLng = function () { return this.latLng; }
    this.getMarkerObj = function () { return this.markerObj; }
    this.remove = function () { this.markerObj.setMap(null); }
}

function Line(listOfDots, map, color) {
    this.listOfDots = listOfDots; this.map = map; this.coords = new Array(); this.polylineObj = null;
    if (this.listOfDots.length > 1) {
        var thisObj = this;
        $.each(this.listOfDots, function (index, value) { thisObj.coords.push(value.getLatLng()); });
        this.polylineObj = new google.maps.Polyline({ path: this.coords, strokeColor: color, strokeOpacity: 1.0, strokeWeight: 2, map: this.map });
    }
    this.remove = function () { this.polylineObj.setMap(null); }
}

function Polygon(listOfDots, map, pen, color, fillOpc) {

    this.listOfDots = listOfDots; this.map = map; this.coords = new Array(); this.parent = pen; this.des = 'Hello';
    if (pen != null) { this.resizeHandle = pen.PolygonChangeHandle; }

    var thisObj = this; $.each(this.listOfDots, function (index, value) { thisObj.coords.push(value.getLatLng()); });
    this.polygonObj = new google.maps.Polygon({ paths: this.coords, strokeColor: color, strokeOpacity: 0.8, strokeWeight: 2, fillColor: color, fillOpacity: fillOpc, map: this.map });
    this.remove = function () { this.info.remove(); this.polygonObj.setMap(null); }
    this.getContent = function () { return this.des; }
    this.getPolygonObj = function () { return this.polygonObj; }
    this.getListOfDots = function () { return this.listOfDots; }
    this.getPlots = function () { return this.polygonObj.getPaths(); }
    this.getColor = function () { return this.getPolygonObj().fillColor; }
    this.setColor = function (color) { return this.getPolygonObj().setOptions({ fillColor: color, strokeColor: color, strokeWeight: 2 }); }
    this.info = new Info(this, this.map); this.addListener = function () {
        var info = this.info; var thisPolygon = this.polygonObj;
    }
    this.setEditable = function (val) {
        if (val == true) {
            currentColor = this.getColor();
        }
        this.polygonObj.setEditable(val);
        this.polygonObj.setOptions({ "zIndex": -9999 });
    };
    this.addListener();

    google.maps.event.addListener(this.polygonObj.getPath(), 'set_at', function (event) {
        //        thisObj.resizeHandle();
        //        alert(thisObj.polygonObj;
        thisObj.parent.PolygonChangeHandle();
        //        var area = thisObj.parent.computeArea();
        //        var length = thisObj.parent.computeLength();
        //        var para = new Object();
        //        para.area = area;
        //        para.length = length;
        //        para.color = currentColor;
        //        if (creator.PolygonResizeHandle != null) {
        //            creator.PolygonResizeHandle(para);
        //        }
    });
    google.maps.event.addListener(this.polygonObj.getPath(), 'insert_at', function (event) {
        thisObj.parent.PolygonChangeHandle();
        //        var area = thisObj.parent.computeArea();
        //        var length = thisObj.parent.computeLength();
        //        var para = new Object();
        //        para.area = area;
        //        para.length = length;
        //        para.color = currentColor;
        //        if (creator.PolygonResizeHandle != null) {
        //            creator.PolygonResizeHandle(para);
        //        }
    });
    google.maps.event.addListener(this.polygonObj.getPath(), 'remove_at', function (event) {
        thisObj.parent.PolygonChangeHandle();
        //        var area = thisObj.parent.computeArea();
        //        var length = thisObj.parent.computeLength();
        //        var para = new Object();
        //        para.area = area;
        //        para.length = length;
        //        para.color = currentColor;
        //        para.color = this.polygonObj.getColor()
        //        if (creator.PolygonResizeHandle != null) {
        //            creator.PolygonResizeHandle(para);
        //        }
    });
}

function Info(polygon, map) {
    this.parent = polygon; this.map = map;
    this.color = document.createElement('input');
    this.button = document.createElement('input');
    $(this.button).attr('type', 'button');
    $(this.button).val("Change Color");
    var thisOjb = this;

    this.changeColor = function () { thisOjb.parent.setColor($(thisOjb.color).val()); }
    this.getContent = function () {
        var content = document.createElement('div');
        $(this.color).val(this.parent.getColor());
        $(this.button).click(function () { thisObj.changeColor(); });
        $(content).append(this.color);
        $(content).append(this.button); return content;
    }
    thisObj = this; this.infoWidObj = new google.maps.InfoWindow({
        content: thisObj.getContent()
    });
    this.show = function (latLng) {
        this.infoWidObj.setPosition(latLng);
        this.infoWidObj.open(this.map);
    }
    this.remove = function () { this.infoWidObj.close(); }
}