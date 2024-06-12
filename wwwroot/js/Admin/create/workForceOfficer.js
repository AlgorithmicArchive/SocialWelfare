function GetDesignations(id) {
  fetch("/Admin/GetDesignations")
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        console.log(data);
        let list = ``;
        data.designations.forEach((element) => {
          list += `<option value="${element.designation}">${element.designation}</option>`;
        });
        $("#" + id).append(list);
      }
    });
}

function CreateOfficer(index) {
  return `
            <div class="custom-card mx-auto border border-dark flex-column mt-2 p-3" data-index="${index}">
                <button class="btn btn-danger d-flex mx-auto mb-2 remove">Remove</button>
                <label for="designation-${index}">Officer Designation</label>
                <select class="form-select" name="designation" id="designation-${index}">
                </select>

                <div class="d-flex align-items-center justify-content-between mt-2">
                    <label for="canForward-${index}">Can Forward Application</label>
                    <div class="form-switch">
                        <input class="form-check-input" type="checkbox" id="canForward-${index}">
                    </div>
                </div>
                <div class="d-flex align-items-center justify-content-between mt-2">
                    <label for="canReturn-${index}">Can Return Application</label>
                    <div class="form-switch">
                        <input class="form-check-input" type="checkbox" id="canReturn-${index}">
                    </div>
                </div>
                <div class="d-flex align-items-center justify-content-between mt-2">
                    <label for="canReturnToEdit-${index}">Can Return For Edit Application</label>
                    <div class="form-switch">
                        <input class="form-check-input" type="checkbox" id="canReturnToEdit-${index}">
                    </div>
                </div>
                <div class="d-flex align-items-center justify-content-between mt-2">
                    <label for="canSanction-${index}">Can Sanction Application</label>
                    <div class="form-switch">
                        <input class="form-check-input" type="checkbox" id="canSanction-${index}">
                    </div>
                </div>
                <div class="d-flex align-items-center justify-content-between mt-2">
                    <label for="canUpdate-${index}">Can Update Application</label>
                    <div class="form-switch">
                        <input class="form-check-input" type="checkbox" id="canUpdate-${index}">
                    </div>
                </div>
            </div>
        `;
}

$(document).ready(function () {
  var index = 0;

  $("#addOfficerBtn").click(function () {
    $("#officersContainer").append(CreateOfficer(index));

    // Initialize officer data object and push it to the array
    workForceOfficers.push({
      Designation: "District Social Welfare Officer",
      canForward: false,
      canReturn: false,
      canReturnToEdit: false,
      canSanction: false,
      canUpdate: false,
    });

    GetDesignations("designation-" + index);
    index++;
    if (index >= 3) {
      $("#saveOfficersBtn").removeClass("d-none").addClass("d-flex");
    }
  });

  $(document).on("click", ".remove", function () {
    let indexToRemove = $(this).closest(".custom-card").data("index");
    $(this).closest(".custom-card").remove();

    // Remove officer data object from the array
    workForceOfficers = workForceOfficers.filter(
      (officer) => officer.id !== indexToRemove
    );
    index--;
    if (index < 3)
      $("saveOfficersBtn").removeClass("d-flex").addClass("d-none");
  });

  $(document).on("change", ".form-check-input, .form-select", function () {
    var card = $(this).closest(".custom-card");
    var index = card.data("index");
    var officer = workForceOfficers.find((officer) => officer.id === index);

    if (officer) {
      officer.designation = card.find(`#designation-${index}`).val();
      officer.canForward = card.find(`#canForward-${index}`).is(":checked");
      officer.canReturn = card.find(`#canReturn-${index}`).is(":checked");
      officer.canReturnToEdit = card
        .find(`#canReturnToEdit-${index}`)
        .is(":checked");
      officer.canSanction = card.find(`#canSanction-${index}`).is(":checked");
      officer.canUpdate = card.find(`#canUpdate-${index}`).is(":checked");
    }
  });

  $("#saveOfficersBtn").click(function () {
    const formdata = new FormData();
    formdata.append("serviceId", serviceId);
    formdata.append("workForceOfficers", JSON.stringify(workForceOfficers));
    fetch("/Admin/UpdateService", { method: "post", body: formdata })
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          $("#WorkForceOfficer").remove();
          $("#Final").show();
        }
      });
  });
});
