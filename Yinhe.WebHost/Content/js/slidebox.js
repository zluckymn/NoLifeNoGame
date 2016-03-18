$(document).ready(function(){
	recount()
	function recount(){
		$(".SBlist").each(function(index, element) {
			$(this).width($(this).children("li").length*$(this).children("li").width());
		});
	}
	$(window).resize(recount);
    //向右 按钮
    $(".goRight").click(function(){
		var $box= $(this).parents(".Slidebox");
		var $parent =$box.find(".SBlist"); 
		var $wid =$box.find(".SBcontent").width();
		//alert($parent.position().left);
		if( !$parent.is(":animated") ){
			if(($wid-$parent.position().left)>=$parent.width()){  //已经到最后一个版面了,如果再向后，必须跳转到第一个版面。
				$parent.animate({ left : 0}, 800); //通过改变left值，跳转到第一个版面
				
				$box.find(".SBnavlist li:first").addClass("cur").siblings().removeClass("cur");
				$box.find(".SBinfolist li:first").addClass("cur").siblings().removeClass("cur");
			}else{
				$parent.animate({ left : '-='+$wid}, 800);  //通过改变left值，达到每次换一个版面
				$box.find(".cur").removeClass("cur").next().addClass("cur");
			}
		}
		return false;
   });
    //往左 按钮
    $(".goLeft").click(function(){
		var $box= $(this).parents(".Slidebox");
		var $parent =$box.find(".SBlist"); 
		var $wid =$box.find(".SBcontent").width();
		//alert($parent.position().left);
	    if( !$parent.is(":animated") ){
			if( $parent.position().left>=0){  //已经到第一个版面了,如果再向前，必须跳转到最后一个版面。
				$parent.animate({ left : '-='+($parent.width()-$wid)}, 800); //通过改变left值，跳转到最后一个版面
				$box.find(".SBnavlist li:last").addClass("cur").siblings().removeClass("cur");
				$box.find(".SBinfolist li:last").addClass("cur").siblings().removeClass("cur");
			}else{
				$parent.animate({ left : '+='+$wid }, 800);  //通过改变left值，达到每次换一个版面
				$box.find(".cur").removeClass("cur").prev().addClass("cur");
			}
		}
		return false;
    });
	$(".SBnavlist li").mouseenter(function(){
		$(this).addClass("cur").siblings().removeClass("cur");
		
		var $box= $(this).parents(".Slidebox");
		var $parent =$box.find(".SBlist");  
		var $wid =$box.find(".SBcontent").width();
		$parent.stop().animate({ left : -$wid*$(this).index()}, 800);
		$box.find(".SBinfolist li").eq($(this).index()).addClass('cur').siblings().removeClass("cur");
	})
	
	var SlideTimer;
	$(".Slidebox").mouseenter(function(){
		clearInterval(SlideTimer);
	}).mouseleave(function(){
		SlideTimer=setInterval('$(".goRight").trigger("click")',5000);
	}).trigger("mouseout");
})