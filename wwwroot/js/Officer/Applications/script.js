let Applications;
let poolApplications;
let approvalApplications;
let approvalIdList = [];
let poolIdList = [];
let finalList = [];

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
  Applications.approveList.recordsTotal += approvalIdList.length;
  selectedCount("mainButton", Applications.pendingList.recordsTotal);
  selectedCount("approveButton", Applications.approveList.recordsTotal);
  selectedCount("poolButton", Applications.poolList.recordsTotal);
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
  Applications.approveList.recordsTotal -= finalList.length;
  selectedCount("mainButton", Applications.pendingList.recordsTotal);
  selectedCount("approveButton", Applications.approveList.recordsTotal);
  selectedCount("poolButton", Applications.poolList.recordsTotal);
  finalList = [];
  selectedCount("transferToPoolButton", finalList);
  selectedCount("transferBackFromApprove", finalList);
  togglePoolButtons();
  $("#mainButton").click();
}

function transferToPoolList() {
  $("#containerSwitcher").show();
  updatePool("ApproveToPool", finalList, "add");
  $("#approve-parent").prop("checked", false).trigger("change");
  Applications.approveList.recordsTotal -= finalList.length;
  Applications.poolList.recordsTotal += finalList.length;
  selectedCount("mainButton", Applications.pendingList.recordsTotal);
  selectedCount("approveButton", Applications.approveList.recordsTotal);
  selectedCount("poolButton", Applications.poolList.recordsTotal);
  finalList = [];
  selectedCount("transferToPoolButton", finalList);
  selectedCount("transferBackFromApprove", finalList);
  togglePoolButtons();
  $("#poolButton").click();
}

function transferFromPoolToApproveList() {
  $("#containerSwitcher").show();
  updatePool("PoolToApprove", finalList, "add");
  $("#approve-parent").prop("checked", false).trigger("change");
  Applications.poolList.recordsTotal -= finalList.length;
  Applications.approveList.recordsTotal += finalList.length;
  selectedCount("mainButton", Applications.pendingList.recordsTotal);
  selectedCount("approveButton", Applications.approveList.recordsTotal);
  selectedCount("poolButton", Applications.poolList.recordsTotal);
  finalList = [];
  selectedCount("sanctionAll", finalList);
  selectedCount("transferBackFromPool", finalList);
  selectedCount("transferBackToInbox", finalList);
  toggleSanctionButtons();
  $("approveButton").click();
}

function transferBackFromPoolList() {
  $("#containerSwitcher").show();
  updatePool("Pool", finalList, "remove");
  $("#poolList-parent").prop("checked", false).trigger("change");
  Applications.pendingList.recordsTotal += finalList.length;
  Applications.poolList.recordsTotal -= finalList.length;
  selectedCount("mainButton", Applications.pendingList.recordsTotal);
  selectedCount("approveButton", Applications.approveList.recordsTotal);
  selectedCount("poolButton", Applications.poolList.recordsTotal);
  finalList = [];
  selectedCount("sanctionAll", finalList);
  selectedCount("transferBackFromPool", finalList);
  toggleSanctionButtons();
  $("#mainButton").click();
}

function updatePool(listType, idList, action) {
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

  const initializeTable = (type, length) => {
    initializeRecordTables(
      "applicationsTable",
      "/Officer/Applications",
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
    if (applications && applications.data.length > 0) {
      applications.data.forEach((element) =>
        idList.push(element.applicationId)
      );
      if (containerSwitcherVisibility) showContainerSwitcher();
      $("#mainButton").removeClass("btn-secondary").addClass("btn-dark");
      selectedCount("mainButton", Applications.pendingList.recordsTotal);
      selectedCount("approveButton", Applications.approveList.recordsTotal);
      selectedCount("poolButton", Applications.poolList.recordsTotal);
    }
  };

  switch (Applications.type) {
    case "Pending":
      initializeTable(Applications.type, 10);
      if (
        Applications.poolList.data.length > 0 ||
        Applications.approveList.data.length > 0
      ) {
        showContainerSwitcher();
      }
      switchToMainContainer();
      break;
    case "Sent":
    case "Sanction":
      hideContainerSwitcher();
      initializeTable(Applications.type, 10);
      break;
  }

  showListTable();

  processApplications(Applications.poolList, poolIdList, true);
  processApplications(Applications.approveList, poolIdList, true);
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
    SanctionAll(poolIdList, finalList, Applications.ServiceId)
  );

  $("#poolButton").on("click", function () {
    initializeRecordTables(
      "applicationsTable",
      "/Officer/Applications",
      "Pool",
      0,
      1
    );
    switchContainer("PoolContainer", "poolButton");
  });

  $("#mainButton").on("click", function () {
    initializeRecordTables(
      "applicationsTable",
      "/Officer/Applications",
      "Pending",
      0,
      1
    );
    switchContainer("MainContainer", "mainButton");
  });

  $("#approveButton").on("click", function () {
    initializeRecordTables(
      "applicationsTable",
      "/Officer/Applications",
      "Approve",
      0,
      1
    );
    switchContainer("ApproveContainer", "approveButton");
  });
});
