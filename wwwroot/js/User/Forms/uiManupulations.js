function handleDistrictChange(districtSelector, tehsilSelector, blockSelector) {
  $(districtSelector).on("change", function () {
    const id = $(this).val();
    // Update the Tehsil select element
    getTehsils(id).then((option) => {
      $(tehsilSelector).empty().append(option); // Clear existing options and append new ones
    });

    // Update the Block select element
    getBlocks(id).then((option) => {
      $(blockSelector).empty().append(option); // Clear existing options and append new ones
    });
  });
}
function oneStepComplete() {
  $(".bg-success")
    .next(".border-primary")
    .removeClass("border-primary")
    .addClass("border-success")
    .next(".bg-primary")
    .removeClass("bg-primary")
    .addClass("bg-success");
}
function copyPresentToPermanent() {
  $("#PermanentAddress").val($("#PresentAddress").val());
  $("#PermanentDistrict").val($("#PresentDistrict").val());
  $("#PermanentDistrict").trigger("change"); // to trigger and populate the tehsil select tag
  setTimeout(() => {
    $("#PermanentTehsil").val($("#PresentTehsil").val());
    $("#PermanentBlock").val($("#PresentBlock").val());
  }, 500);
  $("#PermanentPanchayatMuncipality").val(
    $("#PresentPanchayatMuncipality").val()
  );
  $("#PermanentVillage").val($("#PresentVillage").val());
  $("#PermanentWard").val($("#PresentWard").val());
  $("#PermanentPincode").val($("#PresentPincode").val());
}
function empytPermanent() {
  $("#PermanentAddress").val("");
  $("#PermanentDistrict").val("Please Select");
  $("#PermanentBlock").val("");
  $("#PermanentPanchayatMuncipality").val("");
  $("#PermanentVillage").val("");
  $("#PermanentWard").val("");
  $("#PermanentPincode").val("");
  $("#PermanentDistrict").trigger("change"); // to trigger and empty the tehsil select tag
}
