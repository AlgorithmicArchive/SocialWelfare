let Applications;
let poolApplications;
let approvalApplications;
let approvalIdList = [];
let poolIdList = [];
let finalList = [];
let serviceId = 0;
let bankDispatchFile = "";

function selectedCount(count,type) {
  $("#selectedCount").val("0");
  $("#selectedCount").val(count);
  const selectOptions={
    inbox:[{value:"ItoA",label:"Transfer To Approve List"}],
    approve:[{value:"AtoP",label:"Transder to Pool"},{value:'AtoI',label:"Transfer to Inbox"}],
    pool:[{value:"PtoA",label:"Transder to Approve List"},{value:'PtoI',label:"Transfer to Inbox"},{value:'SanctionAll',label:"Sanction Application(s)"}]
  }
  const options = selectOptions[type].map(item => {
    return `<option value="${item.value}">${item.label}</option>`;
  });
  $("#actionSelect").empty();
  $("#actionSelect").append(options);
  if(count>0) $("#actionButton").attr('disabled',false);
  else $("#actionButton").attr('disabled',true);
  listCount(Applications.pendingList.recordsTotal,Applications.approveCount,Applications.poolCount);
}

function listCount(inboxCount,approveCount,poolCount){
    $('#inboxCount').text(inboxCount);        // Set the count for Inbox
    $('#approveCount').text(approveCount);      // Set the count for Approve List
    $('#poolCount').text(poolCount);         // Set the count for Pool
}


function ItoA() {
  updatePool("Approve", approvalIdList, "add");
  $("#pending-parent").prop("checked", false).trigger("change");
  Applications.pendingList.recordsTotal -= approvalIdList.length;
  Applications.approveCount += approvalIdList.length;
  approvalIdList = [];
  selectedCount(approvalIdList.length,'inbox');
}

function AtoI() {
  updatePool("Approve", finalList, "remove");
  $("#approve-parent").prop("checked", false).trigger("change");
  Applications.pendingList.recordsTotal += finalList.length;
  Applications.approveCount -= finalList.length;
  finalList = [];
  selectedCount(finalList.length,'approve');
}

function AtoP() {
  updatePool("ApproveToPool", finalList, "add");
  $("#approve-parent").prop("checked", false).trigger("change");
  Applications.approveCount -= finalList.length;
  Applications.poolCount += finalList.length;
  finalList = [];
  selectedCount(finalList.length,'approve')
}

function PtoA() {
  updatePool("PoolToApprove", finalList, "add");
  $("#approve-parent").prop("checked", false).trigger("change");
  Applications.poolCount -= finalList.length;
  Applications.approveCount += finalList.length;
  finalList = [];
  selectedCount(finalList.length,'pool')
}

function PtoI() {
  updatePool("Pool", finalList, "remove");
  $("#poolList-parent").prop("checked", false).trigger("change");
  Applications.pendingList.recordsTotal += finalList.length;
  Applications.poolCount -= finalList.length;
  finalList = [];
  selectedCount(finalList.length,'pool');
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

const showContainerSwitcher = () => {
    $("#containerSwitcher").removeClass("d-none").addClass("d-flex");
};
const hideContainerSwitcher = () => {
  setTimeout(()=>{
    $("#containerSwitcher").removeClass("d-flex").addClass("d-none");
  },100)
};
const showListTable = () => {
  $("#listTable").show();
};

function onSelect(list) {
  Applications = list;
  console.log(Applications);
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
  
  const processApplications = (
    applications,
    containerSwitcherVisibility
  ) => {
    if (applications > 0) {
           if (containerSwitcherVisibility) showContainerSwitcher();
          $("#mainButton").removeClass("btn-secondary").addClass("btn-dark");
        listCount(Applications.pendingList.recordsTotal,Applications.approveCount,Applications.poolCount);
    }
  };

  switch (Applications.type) {
    case "Pending":
      initializeTable(Applications.type, 10);
      if (Applications.poolCount > 0 || Applications.approveCount > 0) {
        showContainerSwitcher();
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

  processApplications(Applications.poolCount, true);
  processApplications(Applications.approveCount, true);
}

function updateList(elementClass, list, type) {
  const currentVal = $(elementClass).val();
  if ($(elementClass).is(":checked")) {
    if (!list.includes(currentVal)) {
      list.push(currentVal);
    }
  } else {
    list = list.filter((item) => item !== currentVal);
  }
  selectedCount(list.length, type);
  return list;
}

function toggleAll(parentClass, elementClass) {
  const isChecked = $(parentClass).is(":checked");
  $(elementClass).prop("checked", isChecked).trigger("change");
}


$(document).ready(function () {

  $(document).on("change", ".pending-element", function () {
    approvalIdList = updateList(this, approvalIdList, "inbox");
  });
  $(document).on("change", ".pending-parent", function () {
    toggleAll(this, ".pending-element");
  });
  $(document).on("change", ".approve-element", function () {
    finalList = updateList(this, finalList, "approve");
  });
  $(document).on("change", ".approve-parent", function () {
    toggleAll(this, ".approve-element");
  });
  $(document).on("change", ".poolList-element", function () {
    finalList = updateList(this, finalList, "pool");
  });
  $(document).on("change", ".poolList-parent", function () {
    toggleAll(this, ".poolList-element");
  });
  $("#sanctionAll").on("click", () =>
    SanctionAll(poolIdList, finalList, serviceId)
  );

 
  $(document).on('click','#actionButton',function(){
    const selectedAction = $("#actionSelect").val();
    const actions = {
      'ItoA': ItoA,
      'AtoP': AtoP,
      'AtoI': AtoI,
      'PtoA': PtoA,
      'PtoI': PtoI,
      'SanctionAll':SanctionAll
    };
    if (actions[selectedAction]) {
      if(selectedAction=="SanctionAll")
          actions[selectedAction](poolIdList, finalList, serviceId);  // Call the corresponding function
      else actions[selectedAction]();
    } else {
      console.log('Invalid action selected');
    }
    setTimeout(()=>{
      selectedAction.charAt(0) == 'I'? $('.list-button[value="Inbox"]').trigger('click'):selectedAction.charAt(0)=='P'?$('.list-button[value="Pool"]').trigger('click'):$('.list-button[value="Approve"]').trigger('click');
    },500);
  })

  $(document).on('click', '.list-button', function() {
    const value = $(this).val();
    initializeRecordTables(
      "applicationsTable",
      "/Officer/Applications",
      serviceId,
      value === "Inbox" ? "Pending" : value,
      0,
      10
    );
    $("#currentTable").text(`${value} List`);
    setTimeout(() => {
      showContainerSwitcher();
      listCount(Applications.pendingList.recordsTotal, Applications.approveCount, Applications.poolCount);
      $('.list-button').removeClass('btn-dark').addClass('btn-secondary');
      $(`.list-button[value="${value}"]`).removeClass('btn-secondary').addClass('btn-dark');
    }, 100);
});



});
