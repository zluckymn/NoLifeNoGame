
	function navbarResize(){
		$('.stage_box').width($('.stage_box li').length*($('.stage_box li').width()+6));
	}
	function imgMargin(){
		var $obj=$('.pic_show_box');
		imgTop=($obj.height()-$obj.find('img').height())*0.42;
		$obj.find('img').css('margin-top',imgTop+'px').fadeIn();
	}
	function boxResize(){
		boxHeight=$(window).height()*0.7;
		$('.multipic_preview').css('top',$(window).height()*0.1+'px');
		if(boxHeight>430){
			$('.pic_show_box').height(boxHeight);
		}
	}
	$('.pic_wrap').mouseenter(function(){
		
		liPos=$('.pic_choose_box .stage_box').position().left+$('.pic_choose_box .current').position().left;
		if(liPos <0){
			$('.pic_choose_box .stage_box').css('left',$('.pic_choose_box .stage_box').position().left-liPos+20);
		}else if(liPos >$('.pic_choose_box').width()){
			$('.pic_choose_box .stage_box').css('left',$('.pic_choose_box .stage_box').position().left-liPos+$('.pic_choose_box').width()-80);
		}
		$('.pic_choose_box').stop().animate({bottom:'0'});
	}).mouseleave(function(){
		$('.pic_choose_box').stop().animate({bottom:'-70'});
	})
	$('.multipic_preview .stage_box li').live('click',function(){
		$pObj=$(this).parents('.multipic_preview');
		$(this).addClass('current').siblings().removeClass('current');
		$pObj.find('.pic_show_box img').attr('src',$(this).attr('lsrc')).hide();
		return false;
	})
	$('.multipic_preview .playerGoright').live('click',function(){
		$pObj=$(this).parents('.multipic_preview');
		if($pObj.find('.stage_box .current').next().length){
			$pObj.find('.stage_box .current').next().trigger('click');
		}else{
			$pObj.find('.stage_box li:first').trigger('click');
		}
		return false;
	})
	$('.multipic_preview .playerGoleft').live('click',function(){
		$pObj=$(this).parents('.multipic_preview');
		if($pObj.find('.stage_box .current').prev().length){
			$pObj.find('.stage_box .current').prev().trigger('click');
		}else{
			$pObj.find('.stage_box li:last').trigger('click');
		}
		return false;
	})
	$('.multipic_preview .arrow_boxL').live('click',function(){
		$pObj=$(this).parents('.pic_choose_box');
		animateW=$pObj.find('.stage_box').position().left+$pObj.width()*0.5;
		if(animateW*1<20){
			$pObj.find('.stage_box').animate({left: animateW+'px'}, "slow");
		}else{
			$pObj.find('.stage_box').animate({left:'20px'}, "slow");
		}
		return false;
	})
	$('.multipic_preview .arrow_boxR').live('click',function(){
		$pObj=$(this).parents('.pic_choose_box');
		animateW=$pObj.find('.stage_box').position().left-$pObj.width()*0.5;
		offsetW=$pObj.width()-$pObj.find('.stage_box').width()-20;
		if(offsetW*1>0){
			return false;
		}
		if(animateW*1>offsetW*1){
			$pObj.find('.stage_box').animate({left:animateW+'px'}, "slow");
		}else{
			$pObj.find('.stage_box').animate({left:offsetW+'px'}, "slow");
		}
		return false;
	
	})
	var $curPicIndex='0';
	$('.multipic_preview .photo_type').live('click',function(){
		//以html为下ajax更新数据
		html='<li lsrc="images/pic0013.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0014.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0013.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0016.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0016.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0014.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0013.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0014.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0013.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0016.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0016.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0014.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0013.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0014.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0013.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0016.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0016.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0014.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0013.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0014.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0013.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0016.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0016.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li><li lsrc="images/pic0014.jpg"><a href="#"><img src="images/pic0027.jpg" /></a></li>';
		$pObj=$(this).parents('.multipic_preview');
		$(this).addClass('photo_type_select').siblings().removeClass('photo_type_select');
		$pObj.find('.stage_box ul').html(html);
		navbarResize()
		$('.multipic_preview .stage_box li').eq($curPicIndex).trigger('click');
		$curPicIndex='0';
		
	})
	$('.multipic_preview .close').live('click',function(){
		$(this).parents('.layer_multipic_preview').hide();
		return false;
	})
	//点击图片弹框显示大图
	$('.showDetail').live('click',function(){
		$('.layer_multipic_preview').show().height($(document).height());
		imgMargin()
		boxResize()
		navbarResize()
		$curPicIndex=$(this).attr('index');
		$('.photo_type').eq($(this).attr('album')).trigger('click');
		
	})
	
	$(window).resize(boxResize);
	//点击Tab
	$(".ZZ_tab_click .ZZ_tab_head").live('click',function(){
		$(this).addClass("ZZ_select").siblings(".ZZ_tab_head").removeClass("ZZ_select");
		$(this).parents('.ZZ_tab_click:first').find(".ZZ_tab_body").eq($(this).index()).addClass("ZZ_cur").siblings(".ZZ_tab_body").removeClass("ZZ_cur");
	})
	//右导航
	$(window).bind('scroll resize', function(e){
		var _scrolltop=$(window).scrollTop();
		var _headerTop = _scrolltop;
		var _colTitBarId = "about"; 
		$(".colTitBar").each(function(){
			var offset = $(this).offset();
			if(_headerTop >= offset.top-250) {
				_colTitBarId = $(this).attr('name');
				//alert(_colTitBarId);
			}
		});
		if(!$(".Laye_div_list li a[href=#"+_colTitBarId+"]").parents('li:first').hasClass('select')) {
			$(".Laye_div_list li a[href=#"+_colTitBarId+"]").parents('li:first').addClass('select').siblings().removeClass('select')
		}
	});
	
	
	//时间轴拖动效果
	$('.timeline_box').mousedown(function (e) {
		$obj=$(this);
		var startX=e.pageX;
		var ulPosX=$obj.find('ul').position().left;
		var maxLen=85*$obj.find('ul li').length-$(this).width();
		$obj.bind('mousemove',function(e){
			var $cObj=$obj.find('ul');
			var mouseX=e.pageX;
			var curX=mouseX-startX+ulPosX;
			//$('.timeline_content_block').text('startX:'+startX+';mouseX:'+mouseX+';curX:'+curX+';maxLen:'+maxLen);
			if(curX<20 && curX>maxLen*-1){
				$cObj.css('left',curX+'px');
				return false;
			}
			
		}).mouseup(function(){
			$(this).unbind('mousemove');
		})
	})
	//回到顶部
	$('.bottom').click(
		function(){
			$('html,body').animate({scrollTop:0});
		}
	)

//显示地图
$('.showMap').click(function () {

    $('.pop').height($('body').height()).show();

    var map = new BMap.Map("baiduMap");              // 创建Map实例
    map.enableScrollWheelZoom();    //启用滚轮放大缩小，默认禁用
    map.enableContinuousZoom();    //启用地图惯性拖拽，默认禁用

    map.addControl(new BMap.NavigationControl());  //添加默认缩放平移控件
    switch ($(this).index('.showMap')) {
        case 0:
            map.centerAndZoom(new BMap.Point(118.192745, 24.534056), 18);  //初始化时，即可设置中心点和地图缩放级别
            var marker1 = new BMap.Marker(new BMap.Point(118.192745, 24.534056));  // 创建标注
            break;
        case 1:
            map.centerAndZoom(new BMap.Point(118.72941, 32.037241), 18);  //初始化时，即可设置中心点和地图缩放级别
            var marker1 = new BMap.Marker(new BMap.Point(118.72941, 32.037241));  // 创建标注
            break;

    }

    var infoWindow2 = new BMap.InfoWindow($('.contact_info').eq($(this).index('.showMap')).html());
    marker1.addEventListener("mouseover", function () { this.openInfoWindow(infoWindow2); });

    map.addOverlay(marker1);

    return false;

})


$('.pop .pop-close').click(
		function () {
		    $(this).parent('.pop').hide();
		}
	) 