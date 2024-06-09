const validationFunctionsList = {
  notEmpty,
  onlyAlphabets,
  onlyDigits,
  specificLength,
  isAgeGreaterThan,
  isEmailValid,
  isDateWithinRange,
  CapitalizeAlphabets,
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
  $(document).on("input blur change", "#" + field.name, function () {
    for (let i = 0; i < validationFunctions.length; i++) {
      let msg = validationFunctions[i](field, $("#" + field.name).val());
      if (msg.length != 0) return;
    }
  });
}

function notEmpty(field, value) {
  let msg = "";
  if (
    ($("#" + field.name).is("select") && value == "Please Select") ||
    value == ""
  ) {
    msg = "This field is required.";
  }
  setErrorSpan(field.name, msg);
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
