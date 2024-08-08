function initializeDataTable(
  tableId,
  containerId,
  data,
  tableType = "",
  pageLength = 10
) {
  // Destroy existing DataTable if it exists
  if ($.fn.DataTable.isDataTable(`#${tableId}`)) {
    $(`#${tableId}`).DataTable().destroy();
  }
  const container = $(`#${containerId}`);
  container.empty();
  data.map((item, index) => {
    let row = `<tr><td>${index + 1}</td>`; // First column for index

    for (const key in item) {
      if (item.hasOwnProperty(key)) {
        row += `<td>${item[key] === "" ? "N/A" : item[key]}</td>`;
      }
    }

    row += `</tr>`;
    container.append(row); // Append each row to the container
  });

  // Reinitialize the DataTable
  const dataTable = $(`#${tableId}`).DataTable({
    responsive: true,
    paging: true,
    searching: true,
    info: true,
    lengthChange: true,
    pageLength: pageLength, // Default number of entries to display
    lengthMenu: [1, 3, 10, 25, 50, 100, 500, 1000], // Options for the user to select from
    pagingType: "full_numbers", // Use full pagination control
    // Customizing pagination text
    language: {
      paginate: {
        first: "First",
        previous: "Previous",
        next: "Next",
        last: "Last",
      },
    },
  });

  dataTable.on("length.dt", function () {
    var length = dataTable.page.len();
    var start = dataTable.page.info().start;
    let type = "";
    if (
      tableType === "Pending" ||
      tableType === "Pool" ||
      tableType === "Approve"
    )
      type = "Pending";

    // Send a request to the backend with the new page length
    fetch(`/Officer/Applications?type=${type}&start=${start}&length=${length}`)
      .then((response) => response.json())
      .then((data) => {
        let list, filteredList;
        switch (type) {
          case "Pending":
            list = data.applicationList.PendingList;
            filteredList = PendingObject(
              list,
              data.applicationList.canSanction
            );
            break;
          case "Pool":
            list = data.applicationList.PoolList;
            filteredList = PoolObject(list);
            break;
          case "Approve":
            list = data.applicationList.ApproveList;
            filteredList = ApproveObject(list);
            break;
        }
        console.log(filteredList, dataTable.clear().rows);
        dataTable.clear().rows.add(filteredList).draw();
        // initializeDataTable(tableId, containerId, filteredList, "", length);
      })
      .catch((error) => {
        console.error("Failed to fetch data from backend:", error);
      });
  });
}

function initializeRecordTables(tableId, url, type, start, length) {
  if ($.fn.DataTable.isDataTable(`#${tableId}`)) {
    $(`#${tableId}`).DataTable().destroy();
    $(`#${tableId} thead`).empty();
    $(`#${tableId} tbody`).empty();
  }

  fetch(url + `?type=${type}&start=${start}&length=${length}`)
    .then((res) => res.json())
    .then((json) => {
      $("#SanctionContainer").removeClass("d-flex").addClass("d-none");
      let applications;
      if (type == "Pending") applications = json.applicationList.pendingList;
      else if (type == "Approve")
        applications = json.applicationList.approveList;
      else if (type == "Pool") applications = json.applicationList.poolList;
      else if (type == "Sent") applications = json.applicationList.sentList;
      else if ((type = "Sanction")) {
        applications = json.applicationList.sanctionList;
        switchContainer("SanctionContainer", "sendToBank");
      }

      const data = applications.data;
      const columns = applications.columns;
      const recordsTotal = applications.recordsTotal;
      const recordsFiltered = applications.recordsFiltered;
      const table = $(`#${tableId}`).DataTable({
        data: data,
        columns: columns,
        destroy: true,
        lengthMenu: [1, 2, 10, 25, 50, 100, 500, 1000],
        pageLength: length,
        dom: "Blfrtip",
        buttons: [
          {
            extend: "colvis",
            text: "Select Columns",
            className: "custom-colvis-button",
            action: function (e, dt, node, config) {
              // Remove any existing customButtonCollection
              $(".custom-button-collection").remove();

              // Create custom checkbox UI
              var columnList = $("<div></div>").css("text-align", "left"); // Ensure left alignment
              dt.columns().every(function (index) {
                var column = this;
                var colvisCheckbox = $("<input>", {
                  type: "checkbox",
                  checked: column.visible(),
                }).css("margin-right", "10px");
                var label = $("<label></label>").text(
                  column.header().innerText
                );

                colvisCheckbox.on("change", function () {
                  column.visible(!column.visible());
                });

                // Append checkbox and label directly
                columnList.append(colvisCheckbox).append(label).append("<br>"); // Add <br> for line break
              });

              // Display the custom checkbox UI
              var customButtonCollection = $(
                '<div class="dt-button-collection custom-button-collection"></div>'
              ).append(columnList);

              // Insert the customButtonCollection after the custom-colvis-button
              $(node)
                .attr("aria-haspopup", "true")
                .after(customButtonCollection);

              // Close the collection when clicking outside
              $(document).on("click", function (event) {
                if (
                  !$(event.target).closest(node).length &&
                  !$(event.target).closest(".custom-button-collection").length
                ) {
                  $(".custom-button-collection").hide();
                }
              });

              // Prevent the collection from closing when clicking inside
              $(customButtonCollection).on("click", function (event) {
                event.stopPropagation();
              });

              // Toggle the customButtonCollection visibility on button click
              $(node).on("click", function (event) {
                event.stopPropagation();
                var customButtonCollectionVisible = $(node)
                  .next(".custom-button-collection")
                  .is(":visible");
                $(".custom-button-collection").hide(); // Hide any other open collections
                if (!customButtonCollectionVisible) {
                  $(node).next(".custom-button-collection").show();
                }
              });
            },
          },
        ],
      });

      $(`#${tableId}_paginate`).remove();
      if (recordsTotal > recordsFiltered) {
        const numOfPages = Math.ceil(recordsTotal / length);
        const ul = $(
          `<ul class="pagination d-flex justify-content-end mt-2" id="tablePagination" number-of-pages="${numOfPages}" page-length=${table.page.len()} table-type=${type}>`
        );
        ul.append(
          `<li class="page-item"><a class="page-link" href="#">Previous</a></li>`
        );
        for (let i = 0; i < numOfPages; i++) {
          start = start == numOfPages ? start - 1 : start;
          ul.append(
            `<li class="page-item${
              i === start ? " active" : ""
            }" style="cursor:pointer"><a class="page-link">${i + 1}</a></li>`
          );
        }
        ul.append(
          `<li class="page-item"><a class="page-link" href="#">Next</a></li>`
        );
        $(`#${tableId}`).after(ul);
      }

      // Remove any previous 'length.dt' event handler
      $(`#${tableId}`).off("length.dt");

      $(`#${tableId}`).on("length.dt", function (e, settings, len) {
        initializeRecordTables(tableId, url, type, 0, len);
      });

      // Remove any previous click event handler on #exportAll
      $("#exportAll").off("click");

      $("#exportAll").on("click", function () {
        var activeButtons = [];
        // Select all button elements in the DataTable
        $(".dt-buttons button").each(function () {
          var $button = $(this);
          // Check if the button is active (you can define your own active condition)
          if ($button.hasClass("active")) {
            // Example condition
            activeButtons.push($button.text());
          }
        });

        console.log(activeButtons);

        // Encode the activeButtons array
        var encodedActiveButtons = encodeURIComponent(
          JSON.stringify(activeButtons)
        );

        fetch(
          `/Officer/DownloadAllData?type=${type}&activeButtons=${encodedActiveButtons}`
        )
          .then((response) => {
            if (!response.ok) {
              throw new Error(
                "Network response was not ok " + response.statusText
              );
            }
            return response.json();
          })
          .then((data) => {
            if (data.filePath) {
              console.log(data);
              const a = document.createElement("a");
              a.href = data.filePath;
              a.download = data.filePath;
              document.body.appendChild(a);
              a.click();
              document.body.removeChild(a);
            } else {
              console.error("File path not returned");
            }
          })
          .catch((error) =>
            console.error(
              "There was a problem with the fetch operation:",
              error
            )
          );
      });
    });
}

function printTable(divId) {
  var tableContent = $("#" + divId)
    .find("table")
    .html();

  if (!tableContent) {
    console.error("No table found in the specified div.");
    return;
  }

  // Desired window size
  var width = 3000;
  var height = 2000;

  // Calculate the position for centering the window
  var left = (screen.width - width) / 2;
  var top = (screen.height - height) / 2;

  var myWindow = window.open(
    "",
    "",
    `width=${width},height=${height},top=${top},left=${left}`
  );
  myWindow.document.write("<html><head><title>Print Table</title>");
  myWindow.document.write(`<style>
          table { border-collapse: seprate; width:100% }
          th, td { border: 2px solid black; padding: 8px; text-align: left; }
          thead { background-color: #f2f2f2; }
          th { border: 1px solid black; }
          @media print {
              @page {
                  size: landscape;
              }
              body {
                  margin: 0;
              }
              a[href]:after {
                  content: none !important;
              }
              @page {
                  margin: 0;
              }
              body {
                  margin: 1.6cm;
              }
          }
      </style>`);
  myWindow.document.write("</head><body><table>");
  myWindow.document.write(tableContent);
  myWindow.document.write("</table></body></html>");
  myWindow.document.close();
  myWindow.focus();
  myWindow.print();
  myWindow.close();
}

function SaveAsPdf(divId, tableId) {
  // Get the content of the historyTable
  var table = document.getElementById(divId).querySelector("#" + tableId);
  var rows = table.querySelectorAll("tr");
  var data = [];

  // Extract headers
  var headers = [];
  rows[0].querySelectorAll("th").forEach((th) => {
    headers.push(th.innerText);
  });

  // Extract data
  for (let i = 1; i < rows.length; i++) {
    var row = [];
    rows[i].querySelectorAll("td").forEach((td) => {
      row.push(td.innerText);
    });
    data.push(row);
  }

  // Create a new jsPDF instance
  const { jsPDF } = window.jspdf;
  const doc = new jsPDF("landscape");

  // Add the table to the PDF using autoTable
  doc.autoTable({
    head: [headers],
    body: data,
    margin: { top: 10, left: 10, right: 10, bottom: 10 },
    theme: "striped",
    styles: { overflow: "linebreak", fontSize: 8 },
    tableWidth: "auto", // Automatically adjust the table width
    pageBreak: "auto",
    showHeader: "everyPage",
  });

  // Save the PDF
  doc.save("document.pdf");
}

function exportTableToExcel(tableId, filename = "exported_table.xlsx") {
  // Select the table
  let table = document.getElementById(tableId);

  // Create a new Workbook
  let wb = XLSX.utils.book_new();

  // Convert the table to a worksheet
  let ws = XLSX.utils.table_to_sheet(table);

  // Get the range of the worksheet
  let range = XLSX.utils.decode_range(ws["!ref"]);

  // Loop through each column to determine the maximum width needed
  for (let C = range.s.c; C <= range.e.c; ++C) {
    let maxWidth = 10; // Minimum width for each column
    for (let R = range.s.r; R <= range.e.r; ++R) {
      let cell_address = { c: C, r: R };
      let cell_ref = XLSX.utils.encode_cell(cell_address);
      let cell = ws[cell_ref];
      if (cell && cell.v) {
        let cellLength = cell.v.toString().length;
        if (cellLength > maxWidth) {
          maxWidth = cellLength;
        }
      }
    }
    // Set the column width
    if (!ws["!cols"]) ws["!cols"] = [];
    ws["!cols"][C] = { wch: maxWidth };
  }

  // Append the worksheet to the workbook
  XLSX.utils.book_append_sheet(wb, ws, "Sheet1");

  // Export the workbook
  XLSX.writeFile(wb, filename);
}

$(document).ready(function () {
  $(document).on("click", ".page-link", function () {
    const $pagination = $(this).closest(".pagination");
    const totalPages = parseInt($pagination.attr("number-of-pages"));
    const $currentPageItem = $(".page-item.active", $pagination);
    let currentPageNo = parseInt($currentPageItem.text());
    const pageNumber = $(this).text();
    const pageLength = parseInt($pagination.attr("page-length"));
    $currentPageItem.removeClass("active");
    const tableType = $pagination.attr("table-type");

    let callFunction = true;

    if (pageNumber === "Next" && currentPageNo < totalPages) {
      currentPageNo += 1;
      $currentPageItem.next().addClass("active");
    } else if (pageNumber === "Previous" && currentPageNo > 1) {
      currentPageNo -= 1;
      $currentPageItem.prev().addClass("active");
    } else if (!isNaN(parseInt(pageNumber))) {
      currentPageNo = parseInt(pageNumber);
      $(".page-item", $pagination).removeClass("active");
      $(this).parent().addClass("active");
    } else {
      $currentPageItem.addClass("active"); // Revert back to current page if invalid click
      callFunction = false;
    }

    if (callFunction) {
      initializeRecordTables(
        "applicationsTable",
        "/Officer/Applications",
        tableType,
        (currentPageNo - 1) * pageLength,
        pageLength
      );
    }
  });
});
