DDI Schema Check
==============
DDI Schema Check is a reporting tool that can check the internal structures and spelling of DDI Lifecycle Schema releases. Changes to the DDI Lifecycle can be tracked from 3.0-RC to 3.2 at <http://github.com/dansmith/ddi>

## Checks
- Validate schema set is DDI Lifecycle.
- Check compilation of the schema as an XML Schema Set.
- Versionables and Maintainables allowing inline or reference usage.
    - Versionables and Maintainables are in a xs:Choice.
    - Versionables and Maintainables in a xs:Choice contain two elements.
    - Versionables and Maintainables in a xs:Choice contain a xxxReference.
- FragmentInstance contains all Versionables and Maintainables.
- Type of Object for references
    - Duplicate Element names detected for referenceable types.
    - Element names detected without a TypeOfObject defined.
- Spell checking
    - Element names
    - Attribute names
    - XSD annotations/documentation
    - Breaking apart CamelCasedWords
    - Allows words to be added to dictionary
    - Uses en-US
    - Highlighting of misspellings in generated reports.

## Contributing Checks
If you would like to contribute a new check, create a new class with a method that takes an [XmlSchemaSet](http://msdn.microsoft.com/en-us/library/system.xml.schema.xmlschemaset.aspx) and returns an html string. Please copy the format of current checks and [Bootstrap 3.0](http://getbootstrap.com/) theme to be consistent.

## Sponsor
This work is sponsored by [Colectica](http://www.colectica.com), creators of DDI Lifecycle based software.

![Colectica](http://colectica.com/sites/colectica.com/files/images/colectica-web.png)

## License
This code is released under the GNU Lesser General Public License.
<http://www.gnu.org/licenses/lgpl.html>

![LGPL3](http://www.gnu.org/graphics/lgplv3-147x51.png)