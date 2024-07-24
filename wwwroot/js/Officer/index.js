$(document).ready(function () {
  console.log(countList);
  $("#pending").text(countList.pending);
  $("#forward").text(countList.forward);
  $("#sanction").text(countList.sanction);
  $("#reject").text(countList.reject);
  $("#return").text(countList.return);

  const canSaction = countList.canSanction;
  const canForward = countList.canForward;

  if (!canSaction) $("#sanction").parent().parent().parent().remove();
  if (!canForward) $("#forward").parent().parent().parent().remove();

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
    } else if (property == "pending") {
      fetch("/Officer/Applications?type=Pending")
        .then((res) => res.json())
        .then((data) => {
          if (data.status) onSelect(data.applicationList);
        });
      // window.location.href = "/Officer/Applications?type=Pending";
    } else if (property == "forward" || property == "return") {
      fetch("/Officer/Applications?type=Sent")
        .then((res) => res.json())
        .then((data) => {
          if (data.status) onSelect(data.applicationList);
        });
    } else if (property == "sanction") {
      fetch("/Officer/Applications?type=Sanction")
        .then((res) => res.json())
        .then((data) => {
          if (data.status) onSelect(data.applicationList);
        });
    } else if (property == "reject") {
      fetch("/Officer/Applications?type=Reject")
        .then((res) => res.json())
        .then((data) => {
          if (data.status) onSelect(data.applicationList);
        });
    }
  });
});
