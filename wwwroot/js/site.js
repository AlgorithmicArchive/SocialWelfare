function showSpinner() {
  $("#loadingSpinner").show();
}

function hideSpinner() {
  $("#loadingSpinner").hide();
}
$(document).ready(function () {
  $("#projectsLaunched").hide().fadeOut();
  $(window).scroll(function () {
    var scroll = $(window).scrollTop();

    if ($(document).find("#projectsLaunched").length > 0) {
      var offset =
        $("#projectsLaunched").position().top +
        $("#projectsLaunched").outerHeight() / 2;

      if (scroll > offset && $("#projectsLaunched").not(":visible")) {
        $("#projectsLaunched").stop().fadeIn(500);
      } else if (
        (scroll < offset || $("#projectsLaunched").is(":visible")) &&
        $("#projectsLaunched:visible").length
      ) {
        $("#projectsLaunched").stop().fadeOut();
      }
    }
    if (scroll > 50) {
      $(".fixed-top").addClass("scrolled");
    } else {
      $(".fixed-top").removeClass("scrolled");
    }
  });
});
