import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Label from 'Components/Label';
import Button from 'Components/Link/Button';
import Link from 'Components/Link/Link';
import Menu from 'Components/Menu/Menu';
import MenuContent from 'Components/Menu/MenuContent';
import { kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import AddApplicationPresetMenuItem from './AddApplicationPresetMenuItem';
import styles from './AddApplicationItem.css';

const DEPRECATED_APPLICATIONS = ['Readarr'];
const OBSOLETE_APPLICATIONS = [];

class AddApplicationItem extends Component {

  //
  // Listeners

  onApplicationSelect = () => {
    const {
      implementation,
      implementationName
    } = this.props;

    this.props.onApplicationSelect({ implementation, implementationName });
  };

  //
  // Render

  render() {
    const {
      implementation,
      implementationName,
      infoLink,
      presets,
      onApplicationSelect
    } = this.props;

    const hasPresets = !!presets && !!presets.length;
    const isDeprecated = DEPRECATED_APPLICATIONS.includes(implementation);
    const isObsolete = OBSOLETE_APPLICATIONS.includes(implementation);

    return (
      <div
        className={styles.application}
      >
        <Link
          className={styles.underlay}
          onPress={this.onApplicationSelect}
        />

        <div className={styles.overlay}>
          <div className={styles.name}>
            {implementationName}
            {
              isDeprecated &&
                <Label
                  kind={kinds.WARNING}
                  title={translate('DeprecatedApplicationMessage', { applicationName: implementationName })}
                >
                  {translate('Deprecated')}
                </Label>
            }
            {
              isObsolete &&
                <Label
                  kind={kinds.DANGER}
                  title={translate('ObsoleteApplicationMessage', { applicationName: implementationName })}
                >
                  {translate('Obsolete')}
                </Label>
            }
          </div>

          <div className={styles.actions}>
            {
              hasPresets &&
                <span>
                  <Button
                    size={sizes.SMALL}
                    onPress={this.onApplicationSelect}
                  >
                    Custom
                  </Button>

                  <Menu className={styles.presetsMenu}>
                    <Button
                      className={styles.presetsMenuButton}
                      size={sizes.SMALL}
                    >
                      Presets
                    </Button>

                    <MenuContent>
                      {
                        presets.map((preset) => {
                          return (
                            <AddApplicationPresetMenuItem
                              key={preset.name}
                              name={preset.name}
                              implementation={implementation}
                              implementationName={implementationName}
                              onPress={onApplicationSelect}
                            />
                          );
                        })
                      }
                    </MenuContent>
                  </Menu>
                </span>
            }

            <Button
              to={infoLink}
              size={sizes.SMALL}
            >
              {translate('MoreInfo')}
            </Button>
          </div>
        </div>
      </div>
    );
  }
}

AddApplicationItem.propTypes = {
  implementation: PropTypes.string.isRequired,
  implementationName: PropTypes.string.isRequired,
  infoLink: PropTypes.string.isRequired,
  presets: PropTypes.arrayOf(PropTypes.object),
  onApplicationSelect: PropTypes.func.isRequired
};

export default AddApplicationItem;
