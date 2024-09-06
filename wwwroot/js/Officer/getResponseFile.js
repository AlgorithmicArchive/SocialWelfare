$(document).ready(function () {
  let serviceId = 0;
  let districtId = 0;
  const services = data.serviceList;
  const districts = data.districts;

  services.map((item) => {
    $("#services").append(
      `<option value="${item.serviceId}">${item.serviceName}</option>`
    );
  });

  districts.map((item) => {
    $("#districts").append(
      `<option value="${item.districtId}">${item.districtName}</option>`
    );
  });

  const fields = [
    { id: "services", name: "serviceId", type: "select" },
    { id: "districts", name: "districtId", type: "select" },
    { id: "ftpHost", name: "ftpHost", type: "text" },
    { id: "ftpUser", name: "ftpUser", type: "text" },
    { id: "ftpPassword", name: "ftpPassword", type: "password" },
  ];

  function validateField(field) {
    const $element = $(`#${field.id}`);
    const $errorContainer = $(`#error-${field.id}`);
    let errorMessage = "";

    if (field.type === "select") {
      if ($element.val().trim() === "") {
        errorMessage = `${field.name} is required.`;
      }
    } else {
      if ($element.val().trim() === "") {
        errorMessage = `${field.name} is required.`;
      }
    }

    if (errorMessage) {
      $errorContainer.text(errorMessage);
    } else {
      $errorContainer.text("");
    }
  }

  fields.forEach((field) => {
    const $element = $(`#${field.id}`);
    $element.on("input blur", () => validateField(field));
  });

  $("#submitBtn").on("click", function (event) {
    event.preventDefault(); // Prevent the default form submission

    let allValid = true;
    fields.forEach((field) => {
      const $element = $(`#${field.id}`);
      if ($element.val().trim() === "") {
        validateField(field);
        allValid = false;
      }
    });

    if (allValid) {
      // Create a FormData object
      const formData = new FormData();

      // Append each field value to the FormData object
      fields.forEach((field) => {
        const $element = $(`#${field.id}`);
        formData.append(field.name, $element.val());
      });

      console.log(formData);
    } else {
      console.log("Form has errors. Please check the fields.");
    }
  });

  //   $("#getRecords").on("click", function () {
  //     // const servicesInput = $("#services");
  //     // const districtInput = $("#districts");
  //     // serviceId = servicesInput.val();
  //     // districtId = districtInput.val();
  //     // if (serviceId == 0) {
  //     //   if (!servicesInput.next("span.text-danger").length) {
  //     //     servicesInput.after(
  //     //       `<span class="text-danger" style="font-size:12px;">This field is required</span>`
  //     //     );
  //     //   }
  //     // } else if (districtId == 0) {
  //     //   if (!districtInput.next("span.text-danger").length) {
  //     //     districtInput.after(
  //     //       `<span class="text-danger" style="font-size:12px;">This field is required</span>`
  //     //     );
  //     //   }
  //     // } else {
  //     //   servicesInput.siblings("span.text-danger").remove();
  //     //   districtInput.siblings("span.text-danger").remove();
  //     // }
  //   });
});
