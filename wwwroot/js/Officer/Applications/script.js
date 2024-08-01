let Applications;
let poolApplications;
let approvalApplications;
let approvalIdList = [];
let poolIdList = [];
let finalList = [];

function onSelect(list) {
  Applications = list;
  console.log(Applications);

  switch (Applications.Type) {
    case "Pending":
      PendingTable(Applications);
      PoolTable(Applications);
      ApproveTable(Applications);
      break;
    case "Sent":
      SentTable(Applications);
      break;
    case "Sanction":
      MiscellaneousTable(Applications);
      break;
  }

  $("#listTable").show();

  if (Applications.PoolList && Applications.PoolList.length > 0) {
    poolApplications = Applications.PoolList;
    poolApplications.forEach((element) =>
      poolIdList.push(element.applicationId)
    );
    $("#containerSwitcher").show();
  }

  if (Applications.ApproveList && Applications.ApproveList.length > 0) {
    approvalApplications = Applications.ApproveList;
    approvalApplications.forEach((element) =>
      poolIdList.push(element.applicationId)
    );
    $("#containerSwitcher").show();
  }
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

  $("#sanctionAll").on("click", () =>
    SanctionAll(poolIdList, finalList, Applications.ServiceId)
  );

  $("#poolButton").on("click", function () {
    switchContainer("PoolContainer", "poolButton");
    updateOptionButtons("poolTable");
  });

  $("#mainButton").on("click", function () {
    switchContainer("mainContainer", "mainButton");
    updateOptionButtons("applicationsTable");
  });

  $("#approveButton").on("click", function () {
    switchContainer("ApproveContainer", "approveButton");
    updateOptionButtons("approveTable");
  });

  function toggleTransferButton() {
    const transferButton = $("#mainContainer").find(
      "button:contains('Transfer To Approve List')"
    );
    if (approvalIdList.length > 0) {
      if (transferButton.length === 0) {
        $("#mainContainer").append(`
          <button class="btn btn-dark d-flex mx-auto" id="transferToApproveButton">
            Transfer To Approve List
          </button>
        `);
      }
    } else {
      transferButton.remove();
    }
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
    $("#sanctionAll, #transferBackFromPool").prop("disabled", isDisabled);
  }

  function transferToApproveList() {
    Applications.PendingList = Applications.PendingList.filter((element) => {
      if (approvalIdList.includes(element.applicationId)) {
        Applications.ApproveList.push(element);
        return false;
      }
      return true;
    });
    $("#containerSwitcher").show();
    updatePool("Approve", approvalIdList, "add");
    refreshTables();
    $("#pending-parent").prop("checked", false).trigger("change");
  }

  function transferBackFromApproveList() {
    Applications.ApproveList = Applications.ApproveList.filter((element) => {
      if (finalList.includes(element.applicationId)) {
        Applications.PendingList.push(element);
        return false;
      }
      return true;
    });
    $("#containerSwitcher").show();
    updatePool("Approve", finalList, "remove");
    refreshTables();
    $("#approve-parent").prop("checked", false).trigger("change");
  }

  function transferToPoolList() {
    Applications.ApproveList = Applications.ApproveList.filter((element) => {
      if (finalList.includes(element.applicationId)) {
        Applications.PoolList.push(element);
        return false;
      }
      return true;
    });
    $("#containerSwitcher").show();
    updatePool("ApproveToPool", finalList, "add");
    refreshTables();
    $("#approve-parent").prop("checked", false).trigger("change");
  }

  function transferBackFromPoolList() {
    Applications.PoolList = Applications.PoolList.filter((element) => {
      if (finalList.includes(element.applicationId)) {
        Applications.PendingList.push(element);
        return false;
      }
      return true;
    });
    $("#containerSwitcher").show();
    updatePool("Pool", finalList, "remove");
    refreshTables();
    $("#poolList-parent").prop("checked", false).trigger("change");
  }

  function updatePool(listType, idList, action) {
    const formData = new FormData();
    formData.append("serviceId", Applications.ServiceId);
    formData.append("listType", listType);
    formData.append("IdList", JSON.stringify(idList));
    formData.append("action", action);
    fetch("/Officer/UpdatePool", { method: "post", body: formData })
      .then((res) => res.json())
      .then((data) => {
        if (data.status) {
          console.log(data);
        }
      });
  }

  function refreshTables() {
    PendingTable(Applications);
    PoolTable(Applications);
    ApproveTable(Applications);
  }

  function switchContainer(containerId, buttonId) {
    $("#containerSwitcher")
      .find(".btn-dark")
      .removeClass("btn-dark")
      .addClass("btn-secondary");
    $("#mainContainer, #ApproveContainer, #PoolContainer").hide();
    $(`#${containerId}`).show();
    setTimeout(() => {
      $(`#${buttonId}`).removeClass("btn-secondary").addClass("btn-dark");
      console.log($(`#${buttonId}`).attr("class"));
    }, 100);
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
});
