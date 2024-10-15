const validationFunctionsList = {
  notEmpty,
  onlyAlphabets,
  onlyDigits,
  specificLength,
  isAgeGreaterThan,
  isEmailValid,
  isDateWithinRange,
  CapitalizeAlphabets,
  duplicateAccountNumber,
  validateFile,
};

function setErrorSpan(id, msg) {
  if (!$("#" + id + "Msg").length && msg.length != 0) {
    $("#" + id).after(`<span id="${id}Msg" class="errorMsg">${msg}</span>`);
  } else if (msg.length != 0) {
    $("#" + id + "Msg").empty();
    $("#" + id + "Msg").text(msg);
  } else {
    $("#" + id + "Msg").remove();
  }
}

function attachValidation(field, validationFunctions) {
  const name = field.type == "radio" ? field.name + "Name" : field.name;
  $(document).on("input blur change", "#" + name, function () {
    for (let i = 0; i < validationFunctions.length; i++) {
      let msg = validationFunctions[i](field, $("#" + name).val());
      if (msg.length != 0) return;
    }
  });
}

function notEmpty(field, value) {
  const name = field.type == "radio" ? field.name + "Name" : field.name;
  let msg = "";
  if (($("#" + name).is("select") && value == "Please Select") || value == "") {
    msg = "This field is required.";
  }
  setErrorSpan(name, msg);
  return msg;
}

function onlyAlphabets(field, value) {
  let msg = "";
  if (!/^[A-Za-z .']+$/.test(value)) {
    msg =
      "Please use letters (a-z, A-Z) and special characters (. and ') only.";
  }
  setErrorSpan(field.name, msg);
  return msg;
}

function onlyDigits(field, value) {
  let msg = "";
  if (!/^\d+$/.test(value)) {
    msg = "Please enter only digits";
  }
  setErrorSpan(field.name, msg);
  return msg;
}

function specificLength(filed, value) {
  let msg = "";
  if (value.length != filed.maxLength) {
    msg = `This must be exactly ${filed.maxLength} characters long.`;
  }
  setErrorSpan(filed.name, msg);
  return msg;
}

function isAgeGreaterThan(field, value) {
  let msg = "";
  const currentDate = new Date();
  const compareDate = new Date(
    currentDate.getFullYear() - field.maxLength,
    currentDate.getMonth(),
    currentDate.getDate()
  );
  const inputDate = new Date(value);

  if (inputDate > compareDate) {
    msg = `Age shold be greater than ${field.maxLength}`;
  }
  setErrorSpan(field.name, msg);
  return msg;
}

function isEmailValid(field, value) {
  let msg = "";
  if (!/^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/.test(value)) {
    msg = "Invalid Email Address.";
  }
  setErrorSpan(field.name, msg);
  return msg;
}

function isDateWithinRange(field, value) {
  let msg = "";
  const dateOfMarriage = new Date(value);
  var currentDate = new Date();

  var minDate = new Date(currentDate);
  minDate.setMonth(currentDate.getMonth() + parseInt(field.minLength));

  var maxDate = new Date(currentDate);
  maxDate.setMonth(currentDate.getMonth() + parseInt(field.maxLength));

  if (dateOfMarriage >= minDate && dateOfMarriage <= maxDate) {
    setErrorSpan(field.name, "");
  } else {
    setErrorSpan(
      field.name,
      `The range should be between ${field.minLength} to ${field.maxLength} months from current date.`
    );
  }
  return msg;
}

async function duplicateAccountNumber(id, value, applicationId) {
  let msg = "";
  const res = await fetch(
    "/Base/IsDuplicateAccNo?accNo=" + value + "&applicationId=" + applicationId
  );
  const data = await res.json();
  if (data.status) {
    msg = "Application with this account number already exists.";
  }
  setErrorSpan(id, msg);
  return msg;
}

async function  validateFile(field,value) {
  const id = field.name;
  const fileInput = $(`#${id}`)[0]; // Get the actual DOM element
  const file = fileInput.files[0]; // Get the first selected file
  const formData = new FormData();
  if(field.accept.includes(".jpg"))
    formData.append('fileType','image');
  else if(field.accept.includes(".pdf"))
    formData.append('fileType','pdf');

  formData.append('file',file);
  const res = await fetch("/Base/Validate",{method:'POST',body:formData})  ;
  const data = await res.json();
  if(!data.isValid) setErrorSpan(field.name,data.errorMessage);
  return data.errorMessage;
}

function CapitalizeAlphabets(field, value) {
  let msg = "";
  $("#" + field.name).on("input", function (event) {
    var input = $(this);
    var cursorPosition = input[0].selectionStart;
    var capital = input.val().toUpperCase();
    input.val(capital);
    input[0].setSelectionRange(cursorPosition, cursorPosition);
  });
  return msg;
}
