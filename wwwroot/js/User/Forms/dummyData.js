const indianFemaleNames = [
  "PRIYA SHARMA",
  "ANJALI VERMA",
  "KAVITA MEHTA",
  "NEHA GUPTA",
  "RITU SINGH",
  "SONALI JOSHI",
  "MEGHA PATEL",
  "SNEHA NAIR",
  "SHRUTI MISHRA",
  "NISHA AGARWAL",
  "RUPALI KUMAR",
  "ANITA RAO",
  "LATA IYER",
  "VASUNDHARA REDDY",
  "KIRAN DESAI",
  "RADHA CHOUDHARY",
  "PALLAVI PANDEY",
  "VIDYA MENON",
  "REKHA RATHORE",
  "MADHURI SAXENA",
];
const indianMaleNames = [
  "RAHUL SHARMA",
  "AMIT VERMA",
  "VIKAS MEHTA",
  "RAJEEV GUPTA",
  "ROHIT SINGH",
  "SUNIL JOSHI",
  "KARTIK PATEL",
  "ARJUN NAIR",
  "SAURABH MISHRA",
  "NIKHIL AGARWAL",
  "RAHUL KUMAR",
  "ANIL RAO",
  "VIKRAM IYER",
  "SURESH REDDY",
  "NITIN DESAI",
  "ASHISH CHOUDHARY",
  "PRANAV PANDEY",
  "VINAY MENON",
  "RAJESH RATHORE",
  "DEEPAK SAXENA",
];

const districts = [5, 14, 1];
const tehsils = { 5: [78, 79, 81], 14: [73, 75, 72], 1: [55, 53, 57] };
const blocks = { 5: [56, 57, 58], 14: [124, 125, 126], 1: [1, 2, 3] };

const PresentDistrict = districts[Math.floor(Math.random() * districts.length)];

function getRandomName(namesArray) {
  const randomIndex = Math.floor(Math.random() * namesArray.length);
  return namesArray[randomIndex];
}
function getRandomMarriageDate() {
  const currentDate = new Date();
  const minMonth = 1;
  const maxMonth = 6;

  const addMonths = (date, months) => {
    let d = new Date(date);
    d.setMonth(d.getMonth() + months);
    return d;
  };

  const randomMonths =
    Math.floor(Math.random() * (maxMonth - minMonth + 1)) + minMonth;
  const randomDate = addMonths(currentDate, randomMonths);

  const formatDate = (date) => {
    const day = String(date.getDate()).padStart(2, "0");
    const monthNames = [
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
    const month = monthNames[date.getMonth()];
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
  };

  return formatDate(randomDate);
}

function getRandomBirthDate() {
  const currentDate = new Date();
  const minAge = 20;
  const maxAge = 30;

  // Calculate the range of years for the birthdate
  const minYear = currentDate.getFullYear() - maxAge;
  const maxYear = currentDate.getFullYear() - minAge;

  // Generate a random year within the range
  const randomYear =
    Math.floor(Math.random() * (maxYear - minYear + 1)) + minYear;

  // Generate a random month and day
  const randomMonth = Math.floor(Math.random() * 12); // Months are 0-11 in JavaScript Date
  const randomDay = Math.floor(Math.random() * 28) + 1; // Simplified to avoid dealing with different month lengths

  // Create the random birthdate
  const randomDate = new Date(randomYear, randomMonth, randomDay);

  // Format the date in DD/MMM/YYYY format
  const formatDate = (date) => {
    const day = String(date.getDate()).padStart(2, "0");
    const monthNames = [
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
    const month = monthNames[date.getMonth()];
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
  };

  return formatDate(randomDate);
}
function generateRandomAccountNumber() {
  let accountNumber = "";
  for (let i = 0; i < 16; i++) {
    accountNumber += Math.floor(Math.random() * 10);
  }
  return accountNumber;
}

const generalDummyData = {
  District: districts[Math.floor(Math.random() * districts.length)],
  ApplicantName: getRandomName(indianFemaleNames),
  DateOfBirth: getRandomBirthDate(),
  ApplicantImage: "user-33638_1280.png",
  RelationName: getRandomName(indianMaleNames),
  MotherName: getRandomName(indianFemaleNames),
  DateOfMarriage: getRandomMarriageDate(),
  MobileNumber: "9149653661",
  Email: "momin.rather@gmail.com",
};
const addressDummyData = {
  PresentAddress: "161 GUJJAR NAGAR",
  PresentDistrict: PresentDistrict,
  PresentTehsil:
    tehsils[PresentDistrict][
      Math.floor(Math.random() * tehsils[PresentDistrict].length)
    ],
  PresentBlock:
    blocks[PresentDistrict][
      Math.floor(Math.random() * blocks[PresentDistrict].length)
    ],
  PresentPanchayatMuncipality: "PANCHAYAT",
  PresentVillage: "VILLAGE",
  PresentWard: "WARD",
  PresentPincode: 180001,
};
const bankdummyDetails = {
  BranchName: "RESIDENCY ROAD",
  IfscCode: "JAKA0KEEPER",
  AccountNumber: generateRandomAccountNumber(),
};

const dummyDocuments = [
  "IdentityProofFile",
  "AddressProofFile",
  "RationCardFile",
  "EducationQualificationCertificateFile",
  "ProofofDateofBirthFile",
  "AadharCardFile",
  "DomicileCertificateFile",
  "BankPassbookFile",
  "MarriageCardFile",
  "AffidavitdulyattestedbytheJudicialMagistrateFirstClassFile",
  "ConsentFormforAadharSeedingoftheBankAccountFile",
  "OtherFile",
];

async function fetchFile(url) {
  const response = await fetch(
    `/User/GetFile?filePath=${encodeURIComponent(url)}`
  );
  const blob = await response.blob();
  return new File([blob], url, { type: blob.type });
}

async function appendDummyData(formNo) {
  let dummyData;
  if (formNo === 1) dummyData = generalDummyData;
  if (formNo === 2) dummyData = addressDummyData;
  if (formNo === 3) dummyData = bankdummyDetails;

  if (formNo == 4) {
    try {
      const file = await fetchFile("DummyDocument.pdf");

      dummyDocuments.forEach((id) => {
        const $fileInput = $("#" + id);
        const dataTransfer = new DataTransfer(); // Create a new DataTransfer for each input
        dataTransfer.items.add(file);
        $fileInput[0].files = dataTransfer.files;
      });
    } catch (error) {
      console.error("Error processing the Document file:", error);
    }
  } else {
    // Handle ApplicantImage separately
    if (dummyData.ApplicantImage) {
      try {
        const $fileInput = $("#ApplicantImage");
        const file = await fetchFile(dummyData.ApplicantImage);

        const dataTransfer = new DataTransfer();
        dataTransfer.items.add(file);
        $fileInput[0].files = dataTransfer.files;

        // Trigger change event using jQuery
        $fileInput.trigger("change");
      } catch (error) {
        console.error("Error processing the ApplicantImage file:", error);
      }
    }

    // Handle the rest of the dummy data
    for (const key of Object.keys(dummyData)) {
      if (key !== "ApplicantImage") {
        $("#" + key).val(dummyData[key]);
        if (key.toLowerCase().includes("district")) $("#" + key).change();
      }
    }

    if (formNo == 2) $("#SameAsPresent").click();
  }
}
