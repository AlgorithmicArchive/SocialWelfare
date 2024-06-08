const generalDummyData = {
  District: 15,
  ApplicantName: "MOMIN HUSSAIN RATHER",
  DateOfBirth: "2000-05-23",
  RelationName: "MUZAFFAR RATHER",
  MotherName: "RAHILA ROUF",
  DateOfMarriage: new Date(new Date().setMonth(new Date().getMonth() + 2))
    .toISOString()
    .split("T")[0],
  MobileNumber: "9149653661",
  Email: "momin.rather@gmail.com",
};
const addressDummyData = {
  PresentAddress: "161 GUJJAR NAGAR",
  PresentDistrict: 15,
  PresentTehsil: 150,
  PresentBlock: 56,
  PresentPanchayatMuncipality: "PANCHAYAT",
  PresentVillage: "VILLAGE",
  PresentWard: "WARD",
  PresentPincode: 180001,
};
const bankdummyDetails = {
  BranchName: "RESIDENCY ROAD",
  IfscCode: "JAKA0KEEPER",
  AccountNumber: "1829384939292929",
};
function appendDummyData(formNo) {
  let dummyData;
  if (formNo == 1) dummyData = generalDummyData;
  if (formNo == 2) dummyData = addressDummyData;
  if (formNo == 3) dummyData = bankdummyDetails;

  Object.keys(dummyData).forEach(function (key) {
    $("#" + key).val(dummyData[key]);
    if (key.toLowerCase().includes("district")) $("#" + key).change();
  });
}
