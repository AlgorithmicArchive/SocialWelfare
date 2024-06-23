function initializeDataTable(tableId, containerId, data) {
    // Destroy existing DataTable if it exists
    if ($.fn.DataTable.isDataTable(`#${tableId}`)) {
      $(`#${tableId}`).DataTable().destroy();
    }
    const container = $(`#${containerId}`);
    container.empty();
  
    data.map((item, index) => {
      let row = `<tr><td>${index + 1}</td>`; // First column for index
  
      // Loop through each property in the item object and create a table cell for it
      for (const key in item) {
        if (item.hasOwnProperty(key)) {
          row += `<td>${item[key]}</td>`;
        }
      }
  
      row += `</tr>`;
      container.append(row);
    });
  
    // Reinitialize the DataTable
    $(`#${tableId}`).DataTable({
      paging: true,
      searching: true,
      info: true,
      lengthChange: true,
      pageLength: 10, // Default number of entries to display
      lengthMenu: [5, 10, 25, 50, 100], // Options for the user to select from
    });
  }
  
function printTable(divId) {
    var tableContent = $("#" + divId).find("table").html();
  
    if (!tableContent) {
        console.error("No table found in the specified div.");
        return;
    }

    // Desired window size
    var width = 1080;
    var height = 600;
  
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
          table { border-collapse: collapse; }
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
  
  function SaveAsPdf(divId,tableId) {
     // Get the content of the historyTable
     var table = document.getElementById(divId).querySelector("#"+tableId);
     var rows = table.querySelectorAll('tr');
     var data = [];
  
     // Extract headers
     var headers = [];
     rows[0].querySelectorAll('th').forEach(th => {
         headers.push(th.innerText);
     });
  
     // Extract data
     for (let i = 1; i < rows.length; i++) {
         var row = [];
         rows[i].querySelectorAll('td').forEach(td => {
             row.push(td.innerText);
         });
         data.push(row);
     }
  
     // Create a new jsPDF instance
     const { jsPDF } = window.jspdf;
     const doc = new jsPDF('landscape');
  
     // Add the table to the PDF using autoTable
     doc.autoTable({
         head: [headers],
         body: data,
         margin: { top: 10, left: 10, right: 10, bottom: 10 },
         theme: 'striped',
         styles: { overflow: 'linebreak', fontSize: 8 },
         tableWidth: 'auto', // Automatically adjust the table width
         pageBreak:'auto',
         showHeader:'everyPage',
     });
  
     // Save the PDF
     doc.save('document.pdf');
  }
  
  function exportTableToExcel(tableId, filename = 'exported_table.xlsx') {
     // Select the table
     let table = document.getElementById(tableId);
  
     // Create a new Workbook
     let wb = XLSX.utils.book_new();
   
     // Convert the table to a worksheet
     let ws = XLSX.utils.table_to_sheet(table);
 
     // Get the range of the worksheet
     let range = XLSX.utils.decode_range(ws['!ref']);
     
     // Loop through each column to determine the maximum width needed
     for (let C = range.s.c; C <= range.e.c; ++C) {
         let maxWidth = 10; // Minimum width for each column
         for (let R = range.s.r; R <= range.e.r; ++R) {
             let cell_address = {c: C, r: R};
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
         if (!ws['!cols']) ws['!cols'] = [];
         ws['!cols'][C] = {wch: maxWidth};
     }
 
     // Append the worksheet to the workbook
     XLSX.utils.book_append_sheet(wb, ws, 'Sheet1');
   
     // Export the workbook
     XLSX.writeFile(wb, filename);
  }  