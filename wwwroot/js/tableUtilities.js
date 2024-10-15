function initializeDataTable(
  tableId,
  url,
  serviceId,
  officer,
  district,
  type,
  totalCount,
  start,
  length
) {
  if ($.fn.DataTable.isDataTable(`#${tableId}`)) {
    $(`#${tableId}`).DataTable().destroy();
    $(`#${tableId} thead`).empty();
    $(`#${tableId} tbody`).empty();
  }
  let Url =
    url +
    `?type=${type}&start=${start}&length=${length}&totalCount=${totalCount}&serviceId=${serviceId}&officer=${officer}&district=${district}`;

  showSpinner();
  fetch(Url)
    .then((res) => res.json())
    .then((json) => {
      hideSpinner();
      $("#SanctionContainer").removeClass("d-flex").addClass("d-none");
      let applications;

      applications = json.applicationList;

      const data = applications.data;
      const columns = applications.columns;
      const recordsTotal = applications.recordsTotal;
      const recordsFiltered = applications.recordsFiltered;
      let activeButtons = [];
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

                activeButtons.push(label.text());

                colvisCheckbox.on("change", function () {
                  // Toggle column visibility
                  column.visible(!column.visible());

                  // Get the header text of the column and normalize it
                  const headerText = column.header().innerText.trim();

                  // If the column is now hidden, remove the header text from activeButtons
                  if (!column.visible()) {
                    activeButtons = activeButtons.filter(
                      (item) => item.trim() !== headerText
                    );
                  }
                  // If the column is visible and not already in activeButtons, add it
                  else if (!activeButtons.includes(headerText)) {
                    activeButtons.push(headerText);
                  }
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
          `<ul
                class="pagination d-flex justify-content-end mt-2"
                id="tablePagination"
                data-number-of-pages="${numOfPages}"
                data-page-length="${table.page.len()}"
                data-table-type="${type}"
                data-service-id="${serviceId}"
                data-table-id="${tableId}"
                data-url-link="${url}"
            ></ul>`
        );

        ul.append(
          `<li class="page-item"><a class="page-link" href="#">Previous</a></li>`
        );

        const maxPagesToShow = 10; // Adjust the threshold as needed
        let startPage = 0;
        let endPage = numOfPages;

        if (numOfPages > maxPagesToShow) {
          startPage = Math.max(
            0,
            Math.floor(start / length) - Math.floor(maxPagesToShow / 2)
          );
          endPage = Math.min(numOfPages, startPage + maxPagesToShow);

          if (startPage > 0) {
            ul.append(
              `<li class="page-item disabled"><a class="page-link">...</a></li>`
            );
          }
        }

        for (let i = startPage; i < endPage; i++) {
          const isActive = start / length === i ? " active" : "";
          ul.append(
            `<li class="page-item${isActive}" style="cursor:pointer">
                    <a class="page-link">${i + 1}</a>
                </li>`
          );
        }

        if (endPage < numOfPages) {
          ul.append(
            `<li class="page-item disabled"><a class="page-link">...</a></li>`
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
        initializeRecordTables(tableId, url, serviceId, type, 0, len);
      });

      // Remove any previous click event handler on #exportAll
      $("#exportAll").off("click");

      $("#exportAll").on("click", function () {
        // Encode the activeButtons array

        fetch(
          `/Officer/DownloadAllData?type=${type}&activeButtons=${JSON.stringify(
            activeButtons
          )}`
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

      // $(`#${tableId}_filter`).addClass("w-100");
      $(`#${tableId}_filter`).after(
        `<div class="d-flex justify-content-center gap-3 w-100 mt-3"><p><b>Total Records</b>:${recordsTotal}</p> <p><b>Filtered Records</b>:${recordsFiltered}</p></div>`
      );
    });
}

function initializeRecordTables(tableId, url, serviceId, type, start, length) {
  if ($.fn.DataTable.isDataTable(`#${tableId}`)) {
    $(`#${tableId}`).DataTable().destroy();
    $(`#${tableId} thead`).empty();
    $(`#${tableId} tbody`).empty();
  }
  let Url = url + `?type=${type}&start=${start}&length=${length}`;
  if (serviceId != null && serviceId != 0)
    Url += `&serviceId=${parseInt(serviceId)}`;

  console.log(Url);
  // showSpinner();
  fetch(Url)
    .then((res) => res.json())
    .then((json) => {
      hideSpinner();
      $("#SanctionContainer").removeClass("d-flex").addClass("d-none");
      let applications;
      if (type == "Pending") applications = json.applicationList.pendingList;
      else if (type == "Approve")
        applications = json.applicationList.approveList;
      else if (type == "Pool") applications = json.applicationList.poolList;
      else if (type == "Sent") applications = json.applicationList.sentList;
      else if(type =="Reject") applications = json.applicationList.rejectList;
      else if (type == "Sanction") {
        applications = json.applicationList.sanctionList;
        switchContainer("SanctionContainer", "sendToBank");
      } else if (
        type == "Services" ||
        type == "ApplicationStatus" ||
        type == "Incomplete"
      )
        applications = json.obj;

      console.log(applications);
      const data = applications.data;
      const columns = applications.columns;
      const recordsTotal = applications.recordsTotal;
      const recordsFiltered = applications.recordsFiltered;
      let activeButtons = [];
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

                activeButtons.push(label.text());

                colvisCheckbox.on("change", function () {
                  // Toggle column visibility
                  column.visible(!column.visible());

                  // Get the header text of the column and normalize it
                  const headerText = column.header().innerText.trim();

                  // If the column is now hidden, remove the header text from activeButtons
                  if (!column.visible()) {
                    activeButtons = activeButtons.filter(
                      (item) => item.trim() !== headerText
                    );
                  }
                  // If the column is visible and not already in activeButtons, add it
                  else if (!activeButtons.includes(headerText)) {
                    activeButtons.push(headerText);
                  }
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
        columnDefs: [
          {
              // Assuming the button is in the last column (adjust the index as necessary)
              targets: -1,
              render: function(data, type, row, meta) {
                  try {
                      // Try to parse the data as JSON
                      let buttonData = JSON.parse(data);
  
                      // Check if the parsed data has the necessary button properties
                      if (buttonData && buttonData.function && buttonData.parameters) {
                        let formattedString = buttonData.parameters.map(param => `'${param}'`).join(',');
                          return `<button class="btn btn-dark d-flex mx-auto" onclick="${buttonData.function}(${formattedString})">
                                      ${buttonData.buttonText}
                                  </button>`;
                      } else {
                          // If it's not JSON or doesn't contain button details, render it as text
                          return data;
                      }
                  } catch (e) {
                      // If parsing fails, assume it's plain text and render as is
                      return data;
                  }
              }
          }
      ]
      });

      $(`#${tableId}_paginate`).remove();

      if (recordsTotal > recordsFiltered) {
        const numOfPages = Math.ceil(recordsTotal / length);
        const ul = $(
          `<ul
                class="pagination d-flex justify-content-end mt-2"
                id="tablePagination"
                data-number-of-pages="${numOfPages}"
                data-page-length="${table.page.len()}"
                data-table-type="${type}"
                data-service-id="${serviceId}"
                data-table-id="${tableId}"
                data-url-link="${url}"
            ></ul>`
        );

        ul.append(
          `<li class="page-item"><a class="page-link" href="#">Previous</a></li>`
        );

        const maxPagesToShow = 10; // Adjust the threshold as needed
        let startPage = 0;
        let endPage = numOfPages;

        if (numOfPages > maxPagesToShow) {
          startPage = Math.max(
            0,
            Math.floor(start / length) - Math.floor(maxPagesToShow / 2)
          );
          endPage = Math.min(numOfPages, startPage + maxPagesToShow);

          if (startPage > 0) {
            ul.append(
              `<li class="page-item disabled"><a class="page-link">...</a></li>`
            );
          }
        }

        for (let i = startPage; i < endPage; i++) {
          const isActive = start / length === i ? " active" : "";
          ul.append(
            `<li class="page-item${isActive}" style="cursor:pointer">
                    <a class="page-link">${i + 1}</a>
                </li>`
          );
        }

        if (endPage < numOfPages) {
          ul.append(
            `<li class="page-item disabled"><a class="page-link">...</a></li>`
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
        initializeRecordTables(tableId, url, serviceId, type, 0, len);
      });

      // Remove any previous click event handler on #exportAll
      $("#exportAll").off("click");

      $("#exportAll").on("click", function () {
        // Encode the activeButtons array

        fetch(
          `/Officer/DownloadAllData?type=${type}&activeButtons=${JSON.stringify(
            activeButtons
          )}`
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

      // $(`#${tableId}_filter`).addClass("w-100");
      $(`#${tableId}_filter`).after(
        `<div class="d-flex justify-content-center gap-3 w-100 mt-3"><p><b>Total Records</b>:${recordsTotal}</p> <p><b>Filtered Records</b>:${recordsFiltered}</p></div>`
      );
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
    const totalPages = parseInt($pagination.attr("data-number-of-pages"));
    const $currentPageItem = $(".page-item.active", $pagination);
    let currentPageNo = parseInt($currentPageItem.text());
    const pageNumber = $(this).text();
    const pageLength = parseInt($pagination.attr("data-page-length"));
    $currentPageItem.removeClass("active");
    const tableType = $pagination.attr("data-table-type");
    const serviceId = $pagination.attr("data-service-id");
    const tableId = $pagination.attr("data-table-id");
    const url = $pagination.attr("data-url-link");

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
        tableId,
        url,
        parseInt(serviceId) == NaN ? null : parseInt(serviceId),
        tableType,
        (currentPageNo - 1) * pageLength,
        pageLength
      );
    }
  });
});
