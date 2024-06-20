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
          window.location.href = "/Home/Authentication";
        }
      });
  });

  getDistricts().then((options) => {
    $("[id*='district'], [id*='District']").append(options);
    $("#District").trigger("change");
  });

  getDesignations();

  handleDistrictChange("#District", "#Tehsil");

  $(document).on("blur", "input", async function () {
    const id = $(this).attr("id");
    const value = $(this).val();
    let errorList = [];
    if (id == "Username") errorList = await validateUsername(value);
    else if (id == "Password") errorList = validatePassword(value);
    else if (id == "Email") errorList = await validateEmail(value);
    else if (id == "MobileNumber")
      errorList = await validateMobileNumber(value);
    else if (id == "ConfirmPassword" && value != $("#Password").val())
      errorList = ["Confirm Password should be same as Password."];

    AddErrorSpan(id, errorList);
  });

  $("#designation").on("change", function () {
    const designation = $(this).val();
    if (designation == "Division Level Admin") {
      $("#dynamicFields").empty();
      $("#dynamicFields").append(`
               <label for="Select Postioned Division">Select Postioned Division</label>
                <select class="form-select mb-2 border-0 border-bottom rounded-0" name="Division"
                    id="Division">
                    <option value="1">Jammu</option>
                    <option value="2">Kashmir</option>
                </select>
      `);
    } else if (designation == "State Level Admin") {
      $("#dynamicFields").empty();
    } else {
      $("#dynamicFields").empty();
      $("#dynamicFields").append(`
         <label for="Select Postioned District">Select Postioned District</label>
          <select class="form-select mb-2 border-0 border-bottom rounded-0" name="District"
              id="District">
          </select>
          <label for="Select Postioned Tehsil">Select Postioned Tehsil</label>
          <select class="form-select mb-2 border-0 border-bottom rounded-0" name="Tehsil" id="Tehsil">
          </select>
      `);
    }
  });
});
