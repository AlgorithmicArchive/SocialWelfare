async function getDistricts() {
  let options = ``;
  try {
    const res = await fetch("/Home/GetDistricts", { method: "get" });
    const data = await res.json();
    if (data.status) {
      data.districts.forEach((item) => {
        options += `<option value="${item.uuid}">${item.districtName}</option>`;
      });
    }
  } catch (error) {
    console.error("Error fetching districts:", error);
  }
  return options;
}
async function getTehsils(id) {
  let options = ``;
  try {
    const res = await fetch("/Home/GetTehsils?districtId=" + id, {
      method: "get",
    });
    const data = await res.json();
    if (data.status) {
      data.tehsils.forEach((item) => {
        options += `<option value="${item.uuid}">${item.tehsilName}</option>`;
      });
    }
  } catch (error) {
    console.error("Error fetching districts:", error);
  }
  return options;
}

async function getDesignations() {
  let options = ``;
  try {
    const res = await fetch("/Home/GetDesignations", {
      method: "get",
    });
    const data = await res.json();
    if (data.status) {
      data.designations.forEach((item) => {
        options += `<option value="${item.designation}">${item.designation}</option>`;
      });
      $("#designation").append(options);
    }
  } catch (error) {
    console.error("Error fetching districts:", error);
  }
}

function handleDistrictChange(districtSelector, tehsilSelector) {
  $(districtSelector).on("change", function () {
    const id = $(this).val();

    // Update the Tehsil select element
    getTehsils(id).then((option) => {
      $(tehsilSelector).empty().append(option); // Clear existing options and append new ones
    });
  });
}

$(document).ready(function () {
  $("#registration").on("submit", function (e) {
    e.preventDefault();
    const formdata = new FormData(this);
    fetch("/Home/OfficerRegistration", { method: "post", body: formdata })
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          window.location.href = data.url;
        }
      });
  });

  getDistricts().then((options) => {
    $("[id*='district'], [id*='District']").append(options);
    $("#District").trigger("change");
  });

  getDesignations();

  handleDistrictChange("#District", "#Tehsil");

  $(document).on("blur", "input", function () {
    const id = $(this).attr("id");
    const value = $(this).val();
    let errorList = [];
    if (id == "Username") errorList = validateUsername(value);
    else if (id == "Password") errorList = validatePassword(value);
    else if (id == "Email") errorList = validateEmail(value);
    else if (id == "MobileNumber") errorList = validateMobileNumber(value);
    else if (id == "ConfirmPassword" && value != $("#Password").val())
      errorList = ["Confirm Password should be same as Password."];

    AddErrorSpan(id, errorList);
  });
});
