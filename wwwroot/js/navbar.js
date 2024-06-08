$(document).ready(function () {
  $(".dropdown-toggle").click(function () {
    if ($(this).parent().hasClass("dropend")) {
      $(this).parent().removeClass("dropend").addClass("dropdown");
    } else {
      $(this).parent().removeClass("dropdown").addClass("dropend");
    }
  });
  $(document).click(function () {
    if ($(".dropdown-toggle.show").length === 0) {
      $(".dropdown-toggle")
        .parent()
        .removeClass("dropdown")
        .addClass("dropend");
    }
  });
});
