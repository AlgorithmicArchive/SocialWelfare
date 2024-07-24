function AddErrorSpan(id, errorList) {
  if (!$("#" + id + "Msg").length && errorList.length != 0) {
    let msgs = ``;
    errorList.map((item) => {
      msgs += `<p>${item}</p>`;
    });

    $("#" + id).after(`<span id="${id}Msg" class="errorMsg">${msgs}</span>`);
  } else if (errorList.length != 0) {
    $("#" + id + "Msg").empty();
    errorList.map((item) => {
      $("#" + id + "Msg").append(`<p>${item}</p>`);
    });
  } else {attachValidation
    $("#" + id + "Msg").remove();
  }
}

async function UsernameExists(username) {
  try {
    const res = await fetch("/Base/UsernameAlreadyExist?Username=" + username);
    const data = await res.json();
    return data.status;
  } catch (error) {
    console.error("Error checking username existence:", error);
    return false; // Assume the username doesn't exist in case of an error
  }
}
async function EmailExists(Email) {
  try {
    const res = await fetch("/Base/EmailAlreadyExist?Email=" + Email);
    const data = await res.json();
    return data.status;
  } catch (error) {
    console.error("Error checking Email existence:", error);
    return false; // Assume the Email doesn't exist in case of an error
  }
}
async function MobileNumberExists(MobileNumber) {
  try {
    const res = await fetch(
      "/Base/MobileNumberAlreadyExist?MobileNumber=" + MobileNumber
    );
    const data = await res.json();
    return data.status;
  } catch (error) {
    console.error("Error checking MobileNumber existence:", error);
    return false; // Assume the MobileNumber doesn't exist in case of an error
  }
}

async function IsOldPasswordValid(Password) {
  try {
    const res = await fetch("/Base/IsOldPasswordValid?Password=" + Password);
    const data = await res.json();
    return data.status;
  } catch (error) {
    console.error("Error checking Password existence:", error);
    return false; // Assume the Password doesn't exist in case of an error
  }
}

async function validateUsername(value) {
  let errorList = [];

  if (value.length < 5) {
    errorList.push("Username should be of at least 5 characters.");
  } else if (!/^[a-zA-Z0-9]+$/.test(value)) {
    errorList.push("Username should only contain alphabets and digits.");
  } else {
    const usernameExists = await UsernameExists(value);
    if (usernameExists) {
      errorList.push("Username already exists.");
    }
  }

  return errorList;
}

async function validateEmail(value) {
  let errorList = [];
  if (!/^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/.test(value))
    errorList.push("Please enter a valid email id.");
  else {
    const emailExists = await EmailExists(value);
    if (emailExists) {
      errorList.push("Email already exists.");
    }
  }

  return errorList;
}

async function validateMobileNumber(value) {
  let errorList = [];

  if (value.length < 10) errorList.push("Mobile Number should have 10 digits.");
  else if (!/^\d+$/.test(value))
    errorList.push("Mobile Number should only contain digits.");
  else {
    const mobileNumberExists = await MobileNumberExists(value);
    if (mobileNumberExists) {
      errorList.push("Mobile Number already exists.");
    }
  }
  return errorList;
}

async function ValidateOldPassword(value) {
  let errorList = [];
  const validOldPassword = await IsOldPasswordValid(value);
  if (!validOldPassword) {
    errorList.push("Old Password is incorrect.");
  }
  return errorList;
}

function validatePassword(value) {
  let errorList = [];

  if (value.length < 8)
    errorList.push("Password must be at least 8 characters long.");

  // Check for at least one uppercase letter
  if (!/[A-Z]/.test(value))
    errorList.push("Password must contain at least one uppercase letter.");

  // Check for at least one lowercase letter
  if (!/[a-z]/.test(value))
    errorList.push("Password must contain at least one lowercase letter.");

  // Check for at least one digit
  if (!/\d/.test(value))
    errorList.push("Password must contain at least one digit.");

  // Check for at least one special character
  if (!/[!@#$%^&*]/.test(value))
    errorList.push(
      "Password must contain at least one special character from !@#$%^&*."
    );

  return errorList;
}
