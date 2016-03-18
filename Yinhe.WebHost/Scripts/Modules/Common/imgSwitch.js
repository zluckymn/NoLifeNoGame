(function ($) {
    $.fn.imgSwitch = function (options) {
        options = $.extend(true, {
            index: "0",                       //��¼�±꣬Ĭ��ֵΪ0
            _time: "3000", //������ͣ
            oClass: "this", //ͼƬ�±��л���ʽ
            _objClass: "dlClass"  //ulClass ���� dlClass  Ĭ�� dl ��ʽ
        }, options);
        $("." + options._objClass).each(function () {
            var _this = $(this);
            this.autoScoll = function () {
                var index = options.index;
                var timeId;
                var imgs = _this.find("dt").find('img');
                var len = imgs.length;
                var lastId = 0;
                imgs.hover(function () {
                    clearInterval(timeId);
                }, function () {
                    index = $(this).index();
                    timeId = setInterval(function () {
                        autoScoll(index);
                        index++;
                        if (index == len) {
                            index = 0;
                        }
                    }, options._time);
                }).eq(0).trigger("mouseleave");
                $(this).find("dd a").hover(function () {
                    clearInterval(timeId);
                    $(this).addClass(options.oClass).siblings().removeClass(options.oClass);
                    index = $(this).index();
                    imgs.eq(index).fadeIn("4000").parent().siblings().find("img").hide();
                }, function () {
                    index = $(this).index();
                    lastId = index;
                    if (index == len - 1) {
                        lastId = 0;
                    } else {
                        lastId += 1;
                    }
                    imgs.eq(lastId).trigger("mouseleave");
                });
                function autoScoll(index) {
                    _this.find("dd a:eq(" + index + ")").addClass(options.oClass).siblings().removeClass(options.oClass);
                    imgs.eq(index).fadeIn("4000").parent().siblings().find("img").hide();
                };
                $(this).find("dd a:first").addClass("this");
            }
            this.autoScoll();
        })

    }
    return this;  //ʹ��������
})(jQuery);