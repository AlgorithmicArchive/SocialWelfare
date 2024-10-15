function UpdateDetail(column) {
  const columnName = column.charAt(0).toUpperCase() + column.slice(1);
  $("#updateDetailsButton").click();
  $("#updateDetailsContainer").empty();
  $("#updateDetailsContainer").append(`
        <label>${columnName}</label>
        <input type="hidden" name="columnName" value="${columnName}"/>
        <input type="text" id="${column}" name="columnValue" class="form-control" placeholder="${columnName}" required/>
        <button class="btn btn-dark d-flex mx-auto mt-2">Update</button>
  `);
}

function GenerateBackupCodes() {
  fetch("/Profile/GenerateBackupCodes", { method: "get" })
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        window.location.href = data.url;
      }
    });
}

function ChangePassword() {
  $("#updateDetailsButton").click();
  $("#updateDetailsContainer").empty();
  $("#updateDetailsContainer").append(`
        <input type="password" id="oldPassword" name="oldPassword" class="form-control" placeholder="Old Password" required/>
        <input type="password" id="newPassword" name="newPassword" class="form-control" placeholder="New Password" required/>
        <input type="password" id="confirmNewPassword" name="confirmNewPassword" class="form-control" placeholder="Confirm New Password" required/>
        <button class="btn btn-dark d-flex mx-auto mt-2">Update</button>
  `);
}

function ChangeImage(button) {
  $(button).after(`<input id="chooseProfile" type="file" class="form-control" />`);
  $(button).remove(); // Removes the button after inserting the file input
}

$(document).ready(function () {
  
  const { username, email, mobileNumber, designation } = userDetails;
  const userSpecific = JSON.parse(userDetails.userSpecificDetails);
  if(userSpecific.Profile!="") $("#Profile").attr('src',userSpecific.Profile);
  const appendWithIcon = (selector, content) => {
    $("#" + selector + "Text").html(
      `${content} <i class="fa-solid fa-pencil fs-6" role="button" onclick='UpdateDetail("${selector}")'></i>`
    );
  };
  appendWithIcon("username", username);
  appendWithIcon("email", email);
  appendWithIcon("mobileNumber", mobileNumber);
  if (designation != undefined) {
    appendWithIcon("designation", designation);
  } else {
    $("#designationText").parent().remove();
  }

  $("#UpdateColumn").on("submit", function (e) {
    e.preventDefault();
    let formdata = new FormData(this);
    if (formdata.has("newPassword")) {
      formdata = new FormData();
      formdata.append("columnName", "Password");
      formdata.append("columnValue", $("#newPassword").val());
    }

    let errorPersists = false;
    $(".errorMsg").each(function () {
      if ($(this).length) {
        errorPersists = true;
      }
    });

    if (!errorPersists) {
      showSpinner();
      fetch("/Profile/UpdateColumn", { method: "post", body: formdata })
        .then((res) => res.json())
        .then((data) => {
          if (data.status) window.location.href = data.url;
        });
    }
  });

  $(document).on("blur input", "input", async function () {
    const id = $(this).attr("id");
    const value = $(this).val();
    let errorList = [];
    if (id == "username") errorList = await validateUsername(value);
    else if (id == "email") errorList = await validateEmail(value);
    else if (id == "mobileNumber")
      errorList = await validateMobileNumber(value);
    else if (id == "oldPassword") errorList = await ValidateOldPassword(value);
    else if (id == "newPassword") errorList = validatePassword(value);
    else if (id == "confirmNewPassword" && value != $("#newPassword").val())
      errorList = ["Confirm Password should be same as password."];
    AddErrorSpan(id, errorList);
  });

  $(document).on('change',"#chooseProfile",async function(){
    const file = this.files[0]; // Get the selected file
    if (file) {
      const reader = new FileReader(); // Create a FileReader to read the file

      reader.onload = function(e) {
        // Set the image src to the file's data URL
        $('#Profile').attr('src', e.target.result);
      };

      reader.readAsDataURL(file); // Read the file as a data URL
      const formdata = new FormData();
      formdata.append('file',file);
      const res = await fetch('/Profile/ChangeImage',{method:'POST',body:formdata});
      const data = await res.json();
      if(data.isValid){
        $(this).after(`<button class="btn btn-dark" onclick="ChangeImage(this);">Change Image</button>`)
        $(this).remove();
      }
    }
  })
});
