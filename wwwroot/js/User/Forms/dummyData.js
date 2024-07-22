function formatDateToCustomString(date) {
  const months = [
    "Jan",
    "Feb",
    "Mar",
    "Apr",
    "May",
    "Jun",
    "Jul",
    "Aug",
    "Sep",
    "Oct",
    "Nov",
    "Dec",
  ];
  const day = date.getDate().toString().padStart(2, "0");
  const month = months[date.getMonth()];
  const year = date.getFullYear();
  return `${day}/${month}/${year}`;
}
const generalDummyData = {
  District: 5,
  ApplicantName: "MOMIN HUSSAIN RATHER",
  DateOfBirth: "23/Jul/2000",
  RelationName: "MUZAFFAR RATHER",
  MotherName: "RAHILA ROUF",
  DateOfMarriage: formatDateToCustomString(
    new Date(new Date().setMonth(new Date().getMonth() + 2))
  ),
  MobileNumber: "9149653661",
  Email: "momin.rather@gmail.com",
};
const addressDummyData = {
  PresentAddress: "161 GUJJAR NAGAR",
  PresentDistrict: 5,
  PresentTehsil: 79,
  PresentBlock: 57,
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
