let Applications;
let poolApplications;
let approvalApplications;
let approvalIdList = [];
let poolIdList = [];
let finalList = [];
let serviceId = 0;
let bankDispatchFile = "";

function selectedCount(id, arr) {
  let count = Array.isArray(arr) ? arr.length : arr;
  $(`#${id}`).text($(`#${id}`).text().split("(")[0] + `(${count})`);
}

function toggleTransferButton() {
  const isDisabled = approvalIdList.length === 0;
  $("#transferToApproveButton").prop("disabled", isDisabled);
}

function togglePoolButtons() {
  const isDisabled = finalList.length === 0;
  $("#transferToPoolButton, #transferBackFromApprove").prop(
    "disabled",
    isDisabled
  );
}

function toggleSanctionButtons() {
  const isDisabled = finalList.length === 0;
  console.log(isDisabled);
  $("#sanctionAll, #transferBackFromPool,#transferBackToInbox").prop(
    "disabled",
    isDisabled
  );
}

function transferToApproveList() {
  $("#containerSwitcher").show();
  updatePool("Approve", approvalIdList, "add");
  $("#pending-parent").prop("checked", false).trigger("change");
  Applications.pendingList.recordsTotal -= approvalIdList.length;
  Applications.approveCount += approvalIdList.length;
  selectedCount("mainButton", Applications.pendingList.recordsTotal);
  selectedCount("approveButton", Applications.approveCount);
  selectedCount("poolButton", Applications.poolCount);
  approvalIdList = [];
  selectedCount("transferToApproveButton", approvalIdList);
  toggleTransferButton();
  $("#mainButton").click();
}

function transferBackFromApproveList() {
  $("#containerSwitcher").show();
  updatePool("Approve", finalList, "remove");
  $("#approve-parent").prop("checked", false).trigger("change");

  Applications.pendingList.recordsTotal += finalList.length;
  Applications.approveCount -= finalList.length;
  selectedCount("mainButton", Applications.pendingList.recordsTotal);
  selectedCount("approveButton", Applications.approveCount);
  selectedCount("poolButton", Applications.poolCount);
  finalList = [];
  selectedCount("transferToPoolButton", finalList);
  selectedCount("transferBackFromApprove", finalList);
  togglePoolButtons();
  $("#approveButton").click();
}

function transferToPoolList() {
  $("#containerSwitcher").show();
  updatePool("ApproveToPool", finalList, "add");
  $("#approve-parent").prop("checked", false).trigger("change");
  Applications.approveCount -= finalList.length;
  Applications.poolCount += finalList.length;
  selectedCount("mainButton", Applications.pendingList.recordsTotal);
  selectedCount("approveButton", Applications.approveCount);
  selectedCount("poolButton", Applications.poolCount);
  finalList = [];
  selectedCount("transferToPoolButton", finalList);
  selectedCount("transferBackFromApprove", finalList);
  togglePoolButtons();
  $("#approveButton").click();
}

function transferFromPoolToApproveList() {
  $("#containerSwitcher").show();
  updatePool("PoolToApprove", finalList, "add");
  $("#approve-parent").prop("checked", false).trigger("change");
  Applications.poolCount -= finalList.length;
  Applications.approveCount += finalList.length;
  selectedCount("mainButton", Applications.pendingList.recordsTotal);
  selectedCount("approveButton", Applications.approveCount);
  selectedCount("poolButton", Applications.poolCount);
  finalList = [];
  selectedCount("sanctionAll", finalList);
  selectedCount("transferBackFromPool", finalList);
  selectedCount("transferBackToInbox", finalList);
  toggleSanctionButtons();
  $("#poolButton").click();
}

function transferBackFromPoolList() {
  $("#containerSwitcher").show();
  updatePool("Pool", finalList, "remove");
  $("#poolList-parent").prop("checked", false).trigger("change");
  Applications.pendingList.recordsTotal += finalList.length;
  Applications.poolCount -= finalList.length;
  selectedCount("mainButton", Applications.pendingList.recordsTotal);
  selectedCount("approveButton", Applications.approveCount);
  selectedCount("poolButton", Applications.poolCount);
  finalList = [];
  selectedCount("sanctionAll", finalList);
  selectedCount("transferBackFromPool", finalList);
  toggleSanctionButtons();
  $("#poolButton").click();
}

function updatePool(listType, idList, action) {
  console.log(listType, idList, action);
  const formData = new FormData();
  formData.append("serviceId", Applications.serviceId);
  formData.append("listType", listType);
  formData.append("IdList", JSON.stringify(idList));
  formData.append("action", action);
  fetch("/Officer/UpdatePool", { method: "post", body: formData })
    .then((res) => res.json())
    .then((data) => {
      if (data.status) {
        finalList = [];
      }
    });
}

function switchContainer(containerId, buttonId) {
  $("#containerSwitcher")
    .find(".btn-dark")
    .removeClass("btn-dark")
    .addClass("btn-secondary");
  // Hide all the containers and remove the 'd-flex' class
  $("#MainContainer, #ApproveContainer, #PoolContainer")
    .removeClass("d-flex")
    .addClass("d-none");

  // Show the specific container by adding 'd-flex' and removing 'd-none' classes
  $(`#${containerId}`).removeClass("d-none").addClass("d-flex");
  $(`#${buttonId}`).removeClass("btn-secondary").addClass("btn-dark");
}

function updateOptionButtons(tableId) {
  $("#optionButtons").empty().append(`
    <button class="btn btn-info" onclick="printTable('printableArea')">Print</button>
    <button class="btn btn-success" onclick="exportTableToExcel('${tableId}')">EXCEL</button>
    <button class="btn btn-danger" onclick="SaveAsPdf('printableArea', '${tableId}')">PDF</button>
  `);
  $("#poolButton, #mainButton, #approveButton")
    .removeClass("btn-dark")
    .addClass("btn-secondary");
  $(this).removeClass("btn-secondary").addClass("btn-dark");
}

function onSelect(list) {
  Applications = list;
  serviceId = Applications.serviceId;
  bankDispatchFile = Applications.bankFile;
  const initializeTable = (type, length) => {
    initializeRecordTables(
      "applicationsTable",
      "/Officer/Applications",
      serviceId,
      type,
      0,
      length
    );
  };

  const showContainerSwitcher = () => {
    $("#containerSwitcher").show();
  };

  const hideContainerSwitcher = () => {
    $("#containerSwitcher").hide();
    $("#MainContainer").hide();
  };

  const switchToMainContainer = () => {
    switchContainer("MainContainer", "transferToApproveButton");
  };

  const showListTable = () => {
    $("#listTable").show();
  };

  const processApplications = (
    applications,
    idList,
    containerSwitcherVisibility
  ) => {
    if (applications > 0) {
      if (containerSwitcherVisibility) showContainerSwitcher();
      $("#mainButton").removeClass("btn-secondary").addClass("btn-dark");
      selectedCount("mainButton", Applications.pendingList.recordsTotal);
      selectedCount("approveButton", Applications.approveCount);
      selectedCount("poolButton", Applications.poolCount);
    }
  };

  switch (Applications.type) {
    case "Pending":
      initializeTable(Applications.type, 10);
      if (Applications.poolCount > 0 || Applications.approveCount > 0) {
        showContainerSwitcher();
        switchToMainContainer();
      }
      break;
    case "Sent":
    case "Reject":
    case "Sanction":
      hideContainerSwitcher();
      initializeTable(Applications.type, 10);
      bankDispatchFile =
        Applications.type == "Sanction" ? Applications.bankFile : "";
      break;
  }

  showListTable();

  processApplications(Applications.poolCount, poolIdList, true);
  processApplications(Applications.approveCount, poolIdList, true);
}

$(document).ready(function () {
  $(document).on("change", ".pending-element", function () {
    const currentVal = $(this).val();
    if ($(this).is(":checked")) {
      if (!approvalIdList.includes(currentVal)) {
        approvalIdList.push(currentVal);
      }
    } else {
      approvalIdList = approvalIdList.filter((item) => item !== currentVal);
    }
    toggleTransferButton();
    switchContainer("MainContainer", "transferToApproveButton");
    selectedCount("transferToApproveButton", approvalIdList);
  });

  $(document).on("change", ".pending-parent", function () {
    const isChecked = $(this).is(":checked");
    $(".pending-element").prop("checked", isChecked).trigger("change");
  });

  $(document).on("change", ".approve-element", function () {
    const currentVal = $(this).val();
    if ($(this).is(":checked")) {
      if (!finalList.includes(currentVal)) {
        finalList.push(currentVal);
      }
    } else {
      finalList = finalList.filter((item) => item !== currentVal);
    }
    togglePoolButtons();
    selectedCount("transferToPoolButton", finalList);
    selectedCount("transferBackFromApprove", finalList);
  });

  $(document).on("change", ".approve-parent", function () {
    const isChecked = $(this).is(":checked");
    $(".approve-element").prop("checked", isChecked).trigger("change");
  });

  $(document).on("change", ".poolList-parent", function () {
    const isChecked = $(this).is(":checked");
    $(".poolList-element").prop("checked", isChecked).trigger("change");
  });

  $(document).on("change", ".poolList-element", function () {
    const currentVal = $(this).val();
    if ($(this).is(":checked")) {
      if (!finalList.includes(currentVal)) {
        finalList.push(currentVal);
      }
    } else {
      finalList = finalList.filter((item) => item !== currentVal);
    }
    toggleSanctionButtons();
    selectedCount("sanctionAll", finalList);
    selectedCount("transferBackFromPool", finalList);
    selectedCount("transferBackToInbox", finalList);
  });

  $(document).on("click", "#transferToApproveButton", function () {
    transferToApproveList();
  });

  $(document).on("click", "#transferBackFromApprove", function () {
    transferBackFromApproveList();
  });

  $(document).on("click", "#transferToPoolButton", function () {
    transferToPoolList();
  });

  $(document).on("click", "#transferBackFromPool", function () {
    transferBackFromPoolList();
  });

  $(document).on("click", "#transferBackToInbox", function () {
    transferFromPoolToApproveList();
  });

  $("#sanctionAll").on("click", () =>
    SanctionAll(poolIdList, finalList, serviceId)
  );

  $("#poolButton").on("click", function () {
    initializeRecordTables(
      "applicationsTable",
      "/Officer/Applications",
      serviceId,
      "Pool",
      0,
      10
    );
    switchContainer("PoolContainer", "poolButton");
    $("#currentTable").text("Pool List");
  });

  $("#mainButton").on("click", function () {
    initializeRecordTables(
      "applicationsTable",
      "/Officer/Applications",
      serviceId,
      "Pending",
      0,
      10
    );
    switchContainer("MainContainer", "mainButton");
    $("#currentTable").text("Inbox List");

  });

  $("#approveButton").on("click", function () {
    initializeRecordTables(
      "applicationsTable",
      "/Officer/Applications",
      serviceId,
      "Approve",
      0,
      10
    );
    switchContainer("ApproveContainer", "approveButton");
    console.log("Table Heading",$("#currentTable").text());
    $("#currentTable").text("Approve List");

  });
});
