function handleFormAppend(formNo) {
  $("select").removeAttr("disabled");
  $("input").attr("readonly", false);
  const form = $(`#form${formNo}`);
  const currentForm = new FormData(form[0]);

  const appendOrSetFormField = (form, field, value) => {
    if (form.has(field)) form.set(field, value);
    else form.append(field, value);
  };

  const appendCommonFields = (form, fields) => {
    fields.forEach((field) => {
      currentForm.append(field, window[field]);
      appendOrSetFormField(form, field, window[field]);
    });
  };

  switch (formNo) {
    case 1:
      urlInsert = "/User/InsertGeneralDetails";
      urlUpdate = "/User/UpdateGeneralDetails";
      const formData = JSON.parse(serviceContent.formElement);
      const ServiceSpecific = {};
      formData.forEach((section) => {
        section.fields.forEach((field) => {
          if (field.isFormSpecific) {
            const value = currentForm.get(field.name);
            if (value !== null) {
              currentForm.delete(field.name);
              generalForm.delete(field.name);
              ServiceSpecific[field.name] = value;
            }
          }
        });
      });
      currentForm.append("ServiceSpecific", JSON.stringify(ServiceSpecific));
      currentForm.append("ServiceId", serviceContent.serviceId);
      if (!isFormDataEmpty(generalForm)) {
        appendOrSetFormField(
          generalForm,
          "ServiceSpecific",
          JSON.stringify(ServiceSpecific)
        );
        appendOrSetFormField(
          generalForm,
          "ServiceId",
          serviceContent.serviceId
        );
      }
      handleFormData(generalForm, currentForm, urlInsert, urlUpdate, formNo);
      break;
    case 2:
      urlInsert = "/User/InsertAddressDetails";
      urlUpdate = "/User/UpdateAddressDetails";
      if (!isFormDataEmpty(addressForm)) {
        appendCommonFields(addressForm, [
          "ApplicationId",
          "PresentAddressId",
          "PermanentAddressId",
        ]);
      }
      currentForm.append("ApplicationId", ApplicationId);
      handleFormData(addressForm, currentForm, urlInsert, urlUpdate, formNo);
      break;
    case 3:
      urlInsert = "/User/InsertBankDetails";
      urlUpdate = "/User/UpdateBankDetails";
      const bankDetails = {
        BankName: $("#BankName").val(),
        BranchName: $("#BranchName").val(),
        IfscCode: $("#IfscCode").val(),
        AccountNumber: $("#AccountNumber").val(),
      };
      currentForm.append("ApplicationId", ApplicationId);
      if (!isFormDataEmpty(bankForm)) {
        appendOrSetFormField(bankForm, "ApplicationId", ApplicationId);
      }
      handleFormData(bankForm, currentForm, urlInsert, urlUpdate, formNo);
      break;
    case 4:
      urlInsert = "/User/InsertDocuments";
      urlUpdate = "/User/InsertDocuments";
      const workForceOfficers = JSON.parse(serviceContent.workForceOfficers);
      const documents = JSON.parse(serviceContent.formElement)[4].fields;
      const labels = documents.map((item) => item.label.split(" ").join(""));
      currentForm.append("ApplicationId", ApplicationId.split(",")[0]);
      currentForm.append("ServiceId", serviceContent.serviceId);
      currentForm.append("AccessCode", $("#District").val());
      currentForm.append(
        "workForceOfficers",
        JSON.stringify(workForceOfficers)
      );
      currentForm.append("labels", JSON.stringify(labels));
      if (!isFormDataEmpty(documentForm)) {
        appendOrSetFormField(documentForm, "ApplicationId", ApplicationId);
        appendOrSetFormField(
          documentForm,
          "workForceOfficers",
          JSON.stringify(workForceOfficers)
        );
        appendOrSetFormField(documentForm, "labels", JSON.stringify(labels));
      }
      if (application.returnToEdit) {
        currentForm.append("returnToEdit", true);
        appendOrSetFormField(documentForm, "returnToEdit", true);
      }

      handleFormData(documentForm, currentForm, urlInsert, urlUpdate, formNo);
      break;
  }

  if (application.returnToEdit) {
    $("select").each(function () {
      const id = $(this).attr("id");
      const editList = JSON.parse(application.generalDetails.editList);
      if (!editList.includes(id)) {
        $(this).attr("disabled", true);
      }
    });
    $("input").each(function () {
      const id = $(this).attr("id");
      const editList = JSON.parse(application.generalDetails.editList);
      if (!editList.includes(id)) {
        $(this).prop("readonly", true);
      }
    });
  }
}
function handleFormData(
  originalForm,
  currentForm,
  urlInsert,
  urlUpdate,
  formNo
) {
  if (isFormDataEmpty(originalForm)) {
    console.log(currentForm);
    copyFormData(currentForm, originalForm);
    processApplication(originalForm, urlInsert);
  } else if (isEqual(currentForm, originalForm).length != 0) {
    const differingValues = arrayToFormData(isEqual(currentForm, originalForm));
    differingValues.append("ApplicationId", ApplicationId);
    copyFormData(currentForm, originalForm);
    processApplication(formNo == 4 ? originalForm : differingValues, urlUpdate);
  } else if (application.returnToEdit && formNo == 4) {
    const workForceOfficers = JSON.parse(serviceContent.workForceOfficers);
    const formdata = new FormData();
    formdata.append("ApplicationId", ApplicationId);
    formdata.append("workForceOfficers", JSON.stringify(workForceOfficers));
    formdata.append("serviceId", serviceContent.serviceId);
    formdata.append("District", $("#District").val());
    showSpinner();
    fetch("/User/UpdateEditList", { method: "post", body: formdata })
      .then((res) => res.json())
      .then((data) => {
        hideSpinner();
        if (data.status) {
          window.location.href = "/User/ApplicationStatus";
        }
      });
  } else if (formNo == 4) {
    showSpinner();
    originalForm.append("ServiceId", serviceContent.serviceId);
    originalForm.append("AccessCode", $("#District").val());
    fetch("/User/IncompleteApplication?ApplicationId=" + ApplicationId)
      .then((res) => res.json())
      .then((data) => {
        hideSpinner();
        processApplication(originalForm, urlInsert);
      });
  }
}
function processApplication(originalForm, url) {
  console.log(originalForm);
  showSpinner();
  fetch(url, { method: "post", body: originalForm })
    .then((res) => res.json())
    .then((data) => {
      console.log(data);
      if (data.status) {
        hideSpinner();
        ApplicationId =
          data.applicationId !== undefined ? data.applicationId : ApplicationId;
        PresentAddressId =
          data.presentAddressId !== undefined
            ? data.presentAddressId
            : PresentAddressId;
        PermanentAddressId =
          data.permanentAddressId !== undefined
            ? data.permanentAddressId
            : PermanentAddressId;
        if (data.complete) {
          window.location.href =
            "/User/Acknowledgement?RefNo=" +
            encodeURIComponent(data.applicationId);
        }
      }
    })
    .catch((err) => console.log(err));
}
function formDataToArray(formData) {
  const formDataArray = [];
  for (const pair of formData.entries()) {
    formDataArray.push([pair[0], pair[1]]);
  }
  return formDataArray;
}
function arrayToFormData(array) {
  const formData = new FormData();

  array.forEach((item) => {
    formData.append(item.key, item.value);
  });

  return formData;
}
function isEqual(first, second) {
  const array1 = formDataToArray(first);
  const array2 = formDataToArray(second);
  const differingValues = [];

  if (array1.length !== array2.length) {
    return differingValues;
  }

  for (let i = 0; i < array1.length; i++) {
    const [key1, value1] = array1[i];
    const [key2, value2] = array2[i];
    if (key1 !== key2) {
      differingValues.push({ key: key1, value: value1 });
      continue;
    }
    try {
      // Parse JSON strings and compare
      const jsonValue1 = JSON.parse(value1);
      const jsonValue2 = JSON.parse(value2);
      if (JSON.stringify(jsonValue1) !== JSON.stringify(jsonValue2)) {
        differingValues.push({ key: key1, value: value1 });
      }
    } catch (e) {
      // Compare values directly if they are not JSON strings
      if (value1 != value2) {
        differingValues.push({ key: key1, value: value1 });
      }
    }
  }

  return differingValues;
}
function copyFormData(sourceForm, destinationForm) {
  const tempFormData = new FormData();
  for (let pair of destinationForm.entries()) {
    tempFormData.append(pair[0], pair[1]);
  }
  for (let key of tempFormData.keys()) {
    destinationForm.delete(key);
  }
  for (let pair of sourceForm.entries()) {
    destinationForm.append(pair[0], pair[1]);
  }
}
function isFormDataEmpty(formData) {
  return formData.entries().next().done;
}
function isObject(value) {
  return value && typeof value === "object" && value.constructor === Object;
}
