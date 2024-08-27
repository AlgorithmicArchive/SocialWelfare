function SetCards(countList) {
  console.log(countList);
  $("#detailsCards").show();
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
}

$(document).ready(function () {
  const services = serviceList;
  services.map((item) => {
    $("#services").append(
      `<option value="${item.serviceId}">${item.serviceName}</option>`
    );
  });

  $("#getRecords").on("click", function () {
    const servicesInput = $("#services");
    const serviceId = servicesInput.val();

    if (!serviceId) {
      if (!servicesInput.next("span.text-danger").length) {
        servicesInput.after(
          `<span class="text-danger" style="font-size:12px;">This field is required</span>`
        );
      }
    } else {
      servicesInput.siblings("span").remove();
      showSpinner();
      fetch(`/Officer/GetApplicationsList?serviceId=${serviceId}`)
        .then((res) => res.json())
        .then((data) => {
          console.log(data);
          hideSpinner();
          SetCards(data.countList);
        });
    }
  });

  $(".card").on("click", function () {
    const property = $(this).find(".value").attr("id");
    const value = $(this).find(".value").text();

    if (value == 0) {
      const tooltipMessage = `${
        property.charAt(0).toUpperCase() + property.slice(1)
      } has no records.`;
      $(this).attr("data-bs-original-title", tooltipMessage).tooltip("show");
      setTimeout(() => $(this).tooltip("hide"), 2000);
    } else {
      const propertyTypeMap = {
        pending: "Pending",
        forward: "Sent",
        return: "Sent",
        sanction: "Sanction",
        reject: "Reject",
      };

      const type = propertyTypeMap[property];
      if (type) {
        fetch(
          `/Officer/Applications?type=${type}&serviceId=${parseInt(
            $("#services").val()
          )}`
        )
          .then((res) => res.json())
          .then((data) => {
            if (data.status) onSelect(data.applicationList);
          });
      }
    }
  });
});
