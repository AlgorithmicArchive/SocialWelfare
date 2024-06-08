$(document).ready(function () {
  console.log(countList);
  $("#pending").text(countList.pending);
  $("#forward").text(countList.forward);
  $("#sanction").text(countList.sanction);
  $("#reject").text(countList.reject);
  $("#return").text(countList.return);

  $('[data-bs-toggle="tooltip"]').tooltip({
    trigger: "manual", // Prevents automatic display on hover
    placement: "top",
  });

  $(".card").on("click", function () {
    const property = $(this).find(".value").attr("id");
    const value = $(this).find(".value").text();
    if (value == 0) {
      $(this)
        .attr(
          "data-bs-original-title",
          property.charAt(0).toUpperCase() +
            property.slice(1) +
            " has no records."
        )
        .tooltip("show");
      setTimeout(() => {
        $(this).tooltip("hide");
      }, 2000);
    } else if (property == "pending")
      window.location.href = "/Officer/Applications?type=Pending";
    else if (property == "forward" || property == "return")
      window.location.href = "/Officer/Applications?type=Sent";
    else if (property == "sanction")
      window.location.href = "/Officer/Applications?type=Sanction";
    else if (property == "reject")
      window.location.href = "/Officer/Applications?type=Reject";
  });
});
